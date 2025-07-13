using Microsoft.EntityFrameworkCore;
using RAG.Data;
using RAG.Models;
using UglyToad.PdfPig;
using Xceed.Words.NET;

namespace RAG.Services
{
    /// <summary>
    /// Implementazione SQLite del servizio di gestione dati con integrazione S3
    /// </summary>
    public class SqliteDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SqliteDataService> _logger;
        private readonly IS3StorageService _s3StorageService;
        private readonly IUserConfigService _userConfigService;
        private readonly IPineconeService _pineconeService;

        public SqliteDataService(
            ApplicationDbContext context, 
            ILogger<SqliteDataService> logger,
            IS3StorageService s3StorageService,
            IUserConfigService userConfigService,
            IPineconeService pineconeService)
        {
            _context = context;
            _logger = logger;
            _s3StorageService = s3StorageService;
            _userConfigService = userConfigService;
            _pineconeService = pineconeService;
        }

        // User Configuration
        public async Task<UserConfiguration?> GetUserConfigurationAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation($"[GetUserConfigurationAsync] Recupero configurazione per userId: {userId}");
                
                var userConfig = await _context.UserConfigurations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (userConfig != null)
                {
                    // Carica knowledge rules, tone rules e files separatamente
                    userConfig.KnowledgeRules = await GetKnowledgeRulesAllAsync(userId);
                    userConfig.ToneRules = await GetToneRulesAllAsync(userId);
                    userConfig.Files = await GetFilesAllAsync(userId);
                }

                _logger.LogInformation($"[GetUserConfigurationAsync] Configurazione recuperata per userId: {userId}");
                return userConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetUserConfigurationAsync] Errore durante il recupero della configurazione per userId: {userId}");
                throw;
            }
        }

        public async Task<bool> UpdateUserConfigurationAsync(UserConfiguration configuration)
        {
            try
            {
                _logger.LogInformation($"[UpdateUserConfigurationAsync] Aggiornamento configurazione per userId: {configuration.UserId}");
                
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                // Verifica se esiste già
                var existingConfig = await _context.UserConfigurations
                    .FirstOrDefaultAsync(u => u.UserId == configuration.UserId);

                if (existingConfig == null)
                {
                    // Crea nuova configurazione
                    configuration.CreatedAt = DateTime.UtcNow;
                    configuration.UpdatedAt = DateTime.UtcNow;
                    _context.UserConfigurations.Add(configuration);
                }
                else
                {
                    // Aggiorna esistente
                    existingConfig.UpdatedAt = DateTime.UtcNow;
                    _context.Entry(existingConfig).CurrentValues.SetValues(configuration);
                }

                // Elimina knowledge rules, tone rules e files esistenti
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM KnowledgeRules WHERE UserId = {0}", configuration.UserId);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM ToneRules WHERE UserId = {0}", configuration.UserId);
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Files WHERE UserId = {0}", configuration.UserId);

                // Aggiungi nuove knowledge rules
                foreach (var kr in configuration.KnowledgeRules)
                {
                    _context.Entry(kr).Property("UserId").CurrentValue = configuration.UserId;
                    _context.KnowledgeRules.Add(kr);
                }

                // Aggiungi nuove tone rules
                foreach (var tr in configuration.ToneRules)
                {
                    _context.Entry(tr).Property("UserId").CurrentValue = configuration.UserId;
                    _context.ToneRules.Add(tr);
                }

                // Aggiungi nuovi files
                foreach (var file in configuration.Files)
                {
                    _context.Entry(file).Property("UserId").CurrentValue = configuration.UserId;
                    _context.Files.Add(file);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Elimina tutti gli embedding del namespace utente su Pinecone
                await DeleteUserEmbeddingsAsync(configuration.UserId);

                // Upload su S3 dopo il salvataggio nel database
                await UploadConfigurationToS3Async(configuration);

                _logger.LogInformation($"[UpdateUserConfigurationAsync] Configurazione aggiornata con successo per userId: {configuration.UserId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[UpdateUserConfigurationAsync] Errore durante l'aggiornamento della configurazione per userId: {configuration.UserId}");
                throw;
            }
        }

        public async Task<bool> UpdateUserConfigurationGranularAsync(Guid userId, 
            List<KnowledgeRule>? knowledgeRulesToAdd,
            List<ToneRule>? toneRulesToAdd,
            List<RAG.Models.File>? filesToAdd,
            List<Guid>? knowledgeRulesToDelete,
            List<Guid>? toneRulesToDelete,
            List<Guid>? filesToDelete)
        {
            try
            {
                _logger.LogInformation($"[UpdateUserConfigurationGranularAsync] Aggiornamento granulare per userId: {userId}");
                
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Assicurati che la configurazione utente esista
                var existingConfig = await _context.UserConfigurations
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (existingConfig == null)
                {
                    var newConfig = new UserConfiguration
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserConfigurations.Add(newConfig);
                }
                else
                {
                    existingConfig.UpdatedAt = DateTime.UtcNow;
                }

                // Elimina knowledge rules specifiche
                if (knowledgeRulesToDelete?.Any() == true)
                {
                    var rulesToRemove = await _context.KnowledgeRules
                        .Where(kr => knowledgeRulesToDelete.Contains(kr.Id) && 
                                     EF.Property<Guid>(kr, "UserId") == userId)
                        .ToListAsync();
                    _context.KnowledgeRules.RemoveRange(rulesToRemove);
                }

                // Elimina tone rules specifiche
                if (toneRulesToDelete?.Any() == true)
                {
                    var rulesToRemove = await _context.ToneRules
                        .Where(tr => toneRulesToDelete.Contains(tr.Id) && 
                                     EF.Property<Guid>(tr, "UserId") == userId)
                        .ToListAsync();
                    _context.ToneRules.RemoveRange(rulesToRemove);
                }

                // Elimina files specifici
                if (filesToDelete?.Any() == true)
                {
                    var filesToRemove = await _context.Files
                        .Where(f => filesToDelete.Contains(f.Id) && 
                                    EF.Property<Guid>(f, "UserId") == userId)
                        .ToListAsync();
                    _context.Files.RemoveRange(filesToRemove);
                }

                // Aggiungi nuove knowledge rules
                if (knowledgeRulesToAdd?.Any() == true)
                {
                    foreach (var kr in knowledgeRulesToAdd)
                    {
                        _context.KnowledgeRules.Add(kr);
                        _context.Entry(kr).Property("UserId").CurrentValue = userId;
                    }
                }

                // Aggiungi nuove tone rules
                if (toneRulesToAdd?.Any() == true)
                {
                    foreach (var tr in toneRulesToAdd)
                    {
                        _context.ToneRules.Add(tr);
                        _context.Entry(tr).Property("UserId").CurrentValue = userId;
                    }
                }

                // Aggiungi nuovi files
                if (filesToAdd?.Any() == true)
                {
                    foreach (var file in filesToAdd)
                    {
                        _context.Files.Add(file);
                        _context.Entry(file).Property("UserId").CurrentValue = userId;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Elimina tutti gli embedding del namespace utente su Pinecone
                await DeleteUserEmbeddingsAsync(userId);

                // Upload configurazione completa su S3
                var updatedConfig = await GetUserConfigurationAsync(userId);
                if (updatedConfig != null)
                {
                    await UploadConfigurationToS3Async(updatedConfig);
                }

                _logger.LogInformation($"[UpdateUserConfigurationGranularAsync] Aggiornamento granulare completato per userId: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[UpdateUserConfigurationGranularAsync] Errore durante l'aggiornamento granulare per userId: {userId}");
                throw;
            }
        }

        // Knowledge Rules
        public async Task<List<KnowledgeRule>> GetKnowledgeRulesAsync(Guid userId, int page = 1, int pageSize = 5)
        {
            try
            {
                _logger.LogInformation($"[GetKnowledgeRulesAsync] Recupero knowledge rules per userId: {userId}, page: {page}, pageSize: {pageSize}");
                
                var skip = (page - 1) * pageSize;
                var rules = await _context.KnowledgeRules
                    .AsNoTracking()
                    .Where(kr => EF.Property<Guid>(kr, "UserId") == userId)
                    .OrderByDescending(kr => kr.CreatedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation($"[GetKnowledgeRulesAsync] Recuperate {rules.Count} knowledge rules per userId: {userId}");
                return rules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetKnowledgeRulesAsync] Errore durante il recupero delle knowledge rules per userId: {userId}");
                throw;
            }
        }

        public async Task<int> GetKnowledgeRulesCountAsync(Guid userId)
        {
            try
            {
                var count = await _context.KnowledgeRules
                    .Where(kr => EF.Property<Guid>(kr, "UserId") == userId)
                    .CountAsync();

                _logger.LogInformation($"[GetKnowledgeRulesCountAsync] Conteggio knowledge rules per userId: {userId} = {count}");
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetKnowledgeRulesCountAsync] Errore durante il conteggio delle knowledge rules per userId: {userId}");
                throw;
            }
        }

        public async Task<KnowledgeRule?> GetKnowledgeRuleAsync(Guid userId, Guid ruleId)
        {
            try
            {
                var rule = await _context.KnowledgeRules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(kr => kr.Id == ruleId && EF.Property<Guid>(kr, "UserId") == userId);

                _logger.LogInformation($"[GetKnowledgeRuleAsync] Knowledge rule recuperata per userId: {userId}, ruleId: {ruleId}");
                return rule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetKnowledgeRuleAsync] Errore durante il recupero della knowledge rule per userId: {userId}, ruleId: {ruleId}");
                throw;
            }
        }

        public async Task<KnowledgeRule> CreateKnowledgeRuleAsync(Guid userId, CreateKnowledgeRuleRequest request)
        {
            try
            {
                _logger.LogInformation($"[CreateKnowledgeRuleAsync] Creazione knowledge rule per userId: {userId}");
                
                var newRule = new KnowledgeRule
                {
                    Content = request.Content
                };

                _context.Entry(newRule).Property("UserId").CurrentValue = userId;
                _context.KnowledgeRules.Add(newRule);
                await _context.SaveChangesAsync();

                // Elimina tutti gli embedding del namespace utente su Pinecone
                await DeleteUserEmbeddingsAsync(userId);

                // Upload su S3 dopo il salvataggio nel database
                await UploadKnowledgeRuleToS3Async(userId, newRule);

                _logger.LogInformation($"[CreateKnowledgeRuleAsync] Knowledge rule creata con successo per userId: {userId}, ruleId: {newRule.Id}");
                return newRule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[CreateKnowledgeRuleAsync] Errore durante la creazione della knowledge rule per userId: {userId}");
                throw;
            }
        }

        public async Task<bool> UpdateKnowledgeRuleAsync(Guid userId, Guid ruleId, UpdateKnowledgeRuleRequest request)
        {
            try
            {
                _logger.LogInformation($"[UpdateKnowledgeRuleAsync] Aggiornamento knowledge rule per userId: {userId}, ruleId: {ruleId}");
                
                var rule = await _context.KnowledgeRules
                    .FirstOrDefaultAsync(kr => kr.Id == ruleId && EF.Property<Guid>(kr, "UserId") == userId);

                if (rule == null)
                {
                    _logger.LogWarning($"[UpdateKnowledgeRuleAsync] Knowledge rule non trovata per userId: {userId}, ruleId: {ruleId}");
                    return false;
                }

                rule.Content = request.Content;
                rule.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Elimina tutti gli embedding del namespace utente su Pinecone
                await DeleteUserEmbeddingsAsync(userId);

                // Upload su S3 dopo l'aggiornamento nel database
                await UploadKnowledgeRuleToS3Async(userId, rule);

                _logger.LogInformation($"[UpdateKnowledgeRuleAsync] Knowledge rule aggiornata con successo per userId: {userId}, ruleId: {ruleId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[UpdateKnowledgeRuleAsync] Errore durante l'aggiornamento della knowledge rule per userId: {userId}, ruleId: {ruleId}");
                throw;
            }
        }

        public async Task<bool> DeleteKnowledgeRuleAsync(Guid userId, Guid ruleId)
        {
            try
            {
                _logger.LogInformation($"[DeleteKnowledgeRuleAsync] Eliminazione knowledge rule per userId: {userId}, ruleId: {ruleId}");
                
                var rule = await _context.KnowledgeRules
                    .FirstOrDefaultAsync(kr => kr.Id == ruleId && EF.Property<Guid>(kr, "UserId") == userId);

                if (rule == null)
                {
                    _logger.LogWarning($"[DeleteKnowledgeRuleAsync] Knowledge rule non trovata per userId: {userId}, ruleId: {ruleId}");
                    return false;
                }

                _context.KnowledgeRules.Remove(rule);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[DeleteKnowledgeRuleAsync] Knowledge rule eliminata con successo per userId: {userId}, ruleId: {ruleId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DeleteKnowledgeRuleAsync] Errore durante l'eliminazione della knowledge rule per userId: {userId}, ruleId: {ruleId}");
                throw;
            }
        }

        // Tone Rules
        public async Task<List<ToneRule>> GetToneRulesAsync(Guid userId)
        {
            try
            {
                var rules = await GetToneRulesAllAsync(userId);
                _logger.LogInformation($"[GetToneRulesAsync] Recuperate {rules.Count} tone rules per userId: {userId}");
                return rules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetToneRulesAsync] Errore durante il recupero delle tone rules per userId: {userId}");
                throw;
            }
        }

        public async Task<ToneRule?> GetToneRuleAsync(Guid userId, Guid ruleId)
        {
            try
            {
                var rule = await _context.ToneRules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(tr => tr.Id == ruleId && EF.Property<Guid>(tr, "UserId") == userId);

                _logger.LogInformation($"[GetToneRuleAsync] Tone rule recuperata per userId: {userId}, ruleId: {ruleId}");
                return rule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetToneRuleAsync] Errore durante il recupero della tone rule per userId: {userId}, ruleId: {ruleId}");
                throw;
            }
        }

        public async Task<ToneRule> CreateToneRuleAsync(Guid userId, CreateToneRuleRequest request)
        {
            try
            {
                _logger.LogInformation($"[CreateToneRuleAsync] Creazione tone rule per userId: {userId}");
                
                var newRule = new ToneRule
                {
                    Content = request.Content
                };

                _context.Entry(newRule).Property("UserId").CurrentValue = userId;
                _context.ToneRules.Add(newRule);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[CreateToneRuleAsync] Tone rule creata con successo per userId: {userId}, ruleId: {newRule.Id}");
                return newRule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[CreateToneRuleAsync] Errore durante la creazione della tone rule per userId: {userId}");
                throw;
            }
        }

        public async Task<bool> DeleteToneRuleAsync(Guid userId, Guid ruleId)
        {
            try
            {
                _logger.LogInformation($"[DeleteToneRuleAsync] Eliminazione tone rule per userId: {userId}, ruleId: {ruleId}");
                
                var rule = await _context.ToneRules
                    .FirstOrDefaultAsync(tr => tr.Id == ruleId && EF.Property<Guid>(tr, "UserId") == userId);

                if (rule == null)
                {
                    _logger.LogWarning($"[DeleteToneRuleAsync] Tone rule non trovata per userId: {userId}, ruleId: {ruleId}");
                    return false;
                }

                _context.ToneRules.Remove(rule);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[DeleteToneRuleAsync] Tone rule eliminata con successo per userId: {userId}, ruleId: {ruleId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DeleteToneRuleAsync] Errore durante l'eliminazione della tone rule per userId: {userId}, ruleId: {ruleId}");
                throw;
            }
        }

        // Unanswered Questions
        public async Task<List<UnansweredQuestion>> GetUnansweredQuestionsAsync()
        {
            try
            {
                var questions = await _context.UnansweredQuestions
                    .AsNoTracking()
                    .OrderByDescending(q => q.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation($"[GetUnansweredQuestionsAsync] Recuperate {questions.Count} domande non risposte");
                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetUnansweredQuestionsAsync] Errore durante il recupero delle domande non risposte");
                throw;
            }
        }

        public async Task<UnansweredQuestion?> GetUnansweredQuestionAsync(Guid questionId)
        {
            try
            {
                var question = await _context.UnansweredQuestions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(q => q.Id == questionId);

                _logger.LogInformation($"[GetUnansweredQuestionAsync] Domanda recuperata per questionId: {questionId}");
                return question;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetUnansweredQuestionAsync] Errore durante il recupero della domanda per questionId: {questionId}");
                throw;
            }
        }

        public async Task<UnansweredQuestion> CreateUnansweredQuestionAsync(string question, string? context = null)
        {
            try
            {
                _logger.LogInformation("[CreateUnansweredQuestionAsync] Creazione nuova domanda non risposta");
                
                var newQuestion = new UnansweredQuestion
                {
                    Question = question,
                    Context = context
                };

                _context.UnansweredQuestions.Add(newQuestion);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[CreateUnansweredQuestionAsync] Domanda creata con successo, questionId: {newQuestion.Id}");
                return newQuestion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CreateUnansweredQuestionAsync] Errore durante la creazione della domanda");
                throw;
            }
        }

        public async Task<bool> AnswerQuestionAsync(Guid questionId, AnswerQuestionRequest request)
        {
            try
            {
                _logger.LogInformation($"[AnswerQuestionAsync] Risposta alla domanda per questionId: {questionId}");
                
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                var question = await _context.UnansweredQuestions
                    .FirstOrDefaultAsync(q => q.Id == questionId);

                if (question == null)
                {
                    _logger.LogWarning($"[AnswerQuestionAsync] Domanda non trovata per questionId: {questionId}");
                    return false;
                }

                // Rimuovi la domanda dalle non risposte
                _context.UnansweredQuestions.Remove(question);

                // Aggiungi la risposta come knowledge rule
                var knowledgeRequest = new CreateKnowledgeRuleRequest
                {
                    Content = $"Q: {question.Question}\nA: {request.Answer}"
                };

                var newRule = new KnowledgeRule
                {
                    Content = knowledgeRequest.Content
                };

                _context.Entry(newRule).Property("UserId").CurrentValue = request.UserId;
                _context.KnowledgeRules.Add(newRule);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Elimina tutti gli embedding del namespace utente su Pinecone
                await DeleteUserEmbeddingsAsync(request.UserId);

                // Upload su S3 dopo il salvataggio nel database
                await UploadKnowledgeRuleToS3Async(request.UserId, newRule);

                _logger.LogInformation($"[AnswerQuestionAsync] Domanda risposta con successo per questionId: {questionId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AnswerQuestionAsync] Errore durante la risposta alla domanda per questionId: {questionId}");
                throw;
            }
        }

        public async Task<bool> DeleteUnansweredQuestionAsync(Guid questionId)
        {
            try
            {
                _logger.LogInformation($"[DeleteUnansweredQuestionAsync] Eliminazione domanda per questionId: {questionId}");
                
                var question = await _context.UnansweredQuestions
                    .FirstOrDefaultAsync(q => q.Id == questionId);

                if (question == null)
                {
                    _logger.LogWarning($"[DeleteUnansweredQuestionAsync] Domanda non trovata per questionId: {questionId}");
                    return false;
                }

                _context.UnansweredQuestions.Remove(question);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[DeleteUnansweredQuestionAsync] Domanda eliminata con successo per questionId: {questionId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DeleteUnansweredQuestionAsync] Errore durante l'eliminazione della domanda per questionId: {questionId}");
                throw;
            }
        }

        // Metodi privati di supporto
        private async Task<List<KnowledgeRule>> GetKnowledgeRulesAllAsync(Guid userId)
        {
            return await _context.KnowledgeRules
                .AsNoTracking()
                .Where(kr => EF.Property<Guid>(kr, "UserId") == userId)
                .OrderByDescending(kr => kr.CreatedAt)
                .ToListAsync();
        }

        private async Task<List<ToneRule>> GetToneRulesAllAsync(Guid userId)
        {
            return await _context.ToneRules
                .AsNoTracking()
                .Where(tr => EF.Property<Guid>(tr, "UserId") == userId)
                .OrderByDescending(tr => tr.CreatedAt)
                .ToListAsync();
        }

        private async Task<List<RAG.Models.File>> GetFilesAllAsync(Guid userId)
        {
            return await _context.Files
                .AsNoTracking()
                .Where(f => EF.Property<Guid>(f, "UserId") == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        private async Task UploadConfigurationToS3Async(UserConfiguration configuration)
        {
            try
            {
                _logger.LogInformation($"[UploadConfigurationToS3Async] Upload configurazione su S3 per userId: {configuration.UserId}");
                
                // Unifica Files e KnowledgeRules in un unico contenuto testuale
                var unifiedContent = await UnifyFilesAndKnowledgeRulesAsync(configuration);
                
                var fileNameToUpload = "user_config.txt";
                var fileBytes = System.Text.Encoding.UTF8.GetBytes(unifiedContent);
                
                using var stream = new MemoryStream(fileBytes);
                var formFile = new FormFile(stream, 0, fileBytes.Length, "file", fileNameToUpload)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "text/plain"
                };

                // Elimina file precedenti e carica nuovo
                await _s3StorageService.DeleteAllUserFilesAsync(configuration.UserId.ToString());
                await _s3StorageService.UploadFileAsync(configuration.UserId.ToString(), formFile, fileNameToUpload);

                _logger.LogInformation($"[UploadConfigurationToS3Async] Configurazione caricata su S3 per userId: {configuration.UserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[UploadConfigurationToS3Async] Errore durante l'upload su S3 per userId: {configuration.UserId}");
                // Non lanciare l'eccezione per evitare di bloccare l'operazione principale
            }
        }

        /// <summary>
        /// Unifica Files e KnowledgeRules in un unico contenuto testuale
        /// </summary>
        /// <param name="configuration">Configurazione utente</param>
        /// <returns>Contenuto testuale unificato</returns>
        private async Task<string> UnifyFilesAndKnowledgeRulesAsync(UserConfiguration configuration)
        {
            var sb = new System.Text.StringBuilder();
            
            // Estrai testo dai file
            foreach (var file in configuration.Files)
            {
                try
                {
                    string fileText = await ExtractTextFromFileAsync(file);
                    if (!string.IsNullOrWhiteSpace(fileText))
                    {
                        sb.AppendLine(fileText);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"[UnifyFilesAndKnowledgeRulesAsync] Errore nell'estrazione del testo dal file {file.Name}");
                }
            }
            
            // Aggiungi contenuto delle knowledge rules
            foreach (var knowledgeRule in configuration.KnowledgeRules)
            {
                if (!string.IsNullOrWhiteSpace(knowledgeRule.Content))
                {
                    sb.AppendLine(knowledgeRule.Content);
                }
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Estrae il testo da un file basandosi sul ContentType
        /// </summary>
        /// <param name="file">File da processare</param>
        /// <returns>Testo estratto</returns>
        private async Task<string> ExtractTextFromFileAsync(RAG.Models.File file)
        {
            if (string.IsNullOrEmpty(file.Content))
                return string.Empty;

            try
            {
                // Decodifica Base64
                var cleanBase64 = file.Content;
                if (file.Content.Contains(","))
                {
                    cleanBase64 = file.Content.Split(',')[1];
                }
                
                var fileBytes = Convert.FromBase64String(cleanBase64);
                using var stream = new MemoryStream(fileBytes);
                
                // Estrai testo basandosi sul ContentType
                var contentType = file.ContentType.ToLowerInvariant();
                var fileName = file.Name.ToLowerInvariant();
                
                if (contentType.Contains("pdf") || fileName.EndsWith(".pdf"))
                {
                    return ExtractTextFromPdf(stream);
                }
                else if (contentType.Contains("vnd.openxmlformats-officedocument.wordprocessingml.document") || 
                         contentType.Contains("docx") || fileName.EndsWith(".docx"))
                {
                    return ExtractTextFromDocx(stream);
                }
                else if (contentType.Contains("text/plain") || fileName.EndsWith(".txt"))
                {
                    using var reader = new StreamReader(stream);
                    return await reader.ReadToEndAsync();
                }
                else
                {
                    // Fallback: tenta di leggere come testo
                    using var reader = new StreamReader(stream);
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ExtractTextFromFileAsync] Errore nell'estrazione del testo dal file {file.Name}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Estrae il testo da un PDF utilizzando PdfPig
        /// </summary>
        /// <param name="pdfStream">Stream del PDF</param>
        /// <returns>Testo estratto</returns>
        private string ExtractTextFromPdf(Stream pdfStream)
        {
            using var pdf = UglyToad.PdfPig.PdfDocument.Open(pdfStream);
            var sb = new System.Text.StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Estrae il testo da un DOCX utilizzando DocX
        /// </summary>
        /// <param name="docxStream">Stream del DOCX</param>
        /// <returns>Testo estratto</returns>
        private string ExtractTextFromDocx(Stream docxStream)
        {
            using var ms = new MemoryStream();
            docxStream.CopyTo(ms);
            ms.Position = 0;
            using var doc = Xceed.Words.NET.DocX.Load(ms);
            return doc.Text;
        }

        private async Task UploadKnowledgeRuleToS3Async(Guid userId, KnowledgeRule rule)
        {
            try
            {
                _logger.LogInformation($"[UploadKnowledgeRuleToS3Async] Upload knowledge rule su S3 per userId: {userId}, ruleId: {rule.Id}");
                
                // Recupera configurazione completa e upload
                var configuration = await GetUserConfigurationAsync(userId);
                if (configuration != null)
                {
                    await UploadConfigurationToS3Async(configuration);
                }

                _logger.LogInformation($"[UploadKnowledgeRuleToS3Async] Knowledge rule caricata su S3 per userId: {userId}, ruleId: {rule.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[UploadKnowledgeRuleToS3Async] Errore durante l'upload su S3 per userId: {userId}, ruleId: {rule.Id}");
                // Non lanciare l'eccezione per evitare di bloccare l'operazione principale
            }
        }

        private async Task DeleteUserEmbeddingsAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation($"[DeleteUserEmbeddingsAsync] Eliminazione embedding per namespace userId: {userId}");
                
                var success = await _pineconeService.DeleteAllEmbeddingsInNamespaceAsync(userId.ToString());
                
                if (success)
                {
                    _logger.LogInformation($"[DeleteUserEmbeddingsAsync] Embedding eliminati con successo per namespace userId: {userId}");
                }
                else
                {
                    _logger.LogWarning($"[DeleteUserEmbeddingsAsync] Eliminazione embedding fallita per namespace userId: {userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DeleteUserEmbeddingsAsync] Errore durante l'eliminazione degli embedding per namespace userId: {userId}");
                // Non lanciare l'eccezione per evitare di bloccare l'operazione principale
            }
        }
    }
} 