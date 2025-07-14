using Microsoft.EntityFrameworkCore;
using RAG.Data;
using RAG.Entities;
using FileEntity = RAG.Entities.File;

namespace RAG.Services
{
    /// <summary>
    /// Implementazione SQLite del servizio di gestione dati
    /// </summary>
    public class SqliteDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SqliteDataService> _logger;

        public SqliteDataService(
            ApplicationDbContext context, 
            ILogger<SqliteDataService> logger)
        {
            _context = context;
            _logger = logger;
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

        public async Task<bool> UpdateUserConfigurationGranularAsync(Guid userId, 
            List<KnowledgeRule>? knowledgeRulesToAdd,
            List<ToneRule>? toneRulesToAdd,
            List<FileEntity>? filesToAdd,
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
                        _context.Entry(kr).Property("UserId").CurrentValue = userId;
                        _context.KnowledgeRules.Add(kr);
                    }
                }

                // Aggiungi nuove tone rules
                if (toneRulesToAdd?.Any() == true)
                {
                    foreach (var tr in toneRulesToAdd)
                    {
                        _context.Entry(tr).Property("UserId").CurrentValue = userId;
                        _context.ToneRules.Add(tr);
                    }
                }

                // Aggiungi nuovi files
                if (filesToAdd?.Any() == true)
                {
                    foreach (var file in filesToAdd)
                    {
                        _context.Entry(file).Property("UserId").CurrentValue = userId;
                        _context.Files.Add(file);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"[UpdateUserConfigurationGranularAsync] Configurazione aggiornata con successo per userId: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[UpdateUserConfigurationGranularAsync] Errore durante l'aggiornamento della configurazione per userId: {userId}");
                throw;
            }
        }

        // Unanswered Questions
        public async Task<List<UnansweredQuestion>> GetUnansweredQuestionsAsync()
        {
            try
            {
                _logger.LogInformation("[GetUnansweredQuestionsAsync] Recupero lista domande non risposte");
                
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

        public async Task<bool> AnswerQuestionAsync(Guid questionId, AnswerQuestionRequest request)
        {
            try
            {
                _logger.LogInformation($"[AnswerQuestionAsync] Risposta alla domanda per questionId: {questionId}");
                
                var question = await _context.UnansweredQuestions
                    .FirstOrDefaultAsync(q => q.Id == questionId);

                if (question == null)
                {
                    _logger.LogWarning($"[AnswerQuestionAsync] Domanda non trovata per questionId: {questionId}");
                    return false;
                }

                // Crea una nuova knowledge rule dalla coppia Q&A
                var knowledgeRule = new KnowledgeRule
                {
                    Content = $"Domanda: {question.Question}\nRisposta: {request.Answer}"
                };

                // Aggiungi la knowledge rule alla configurazione dell'utente
                _context.Entry(knowledgeRule).Property("UserId").CurrentValue = request.UserId;
                _context.KnowledgeRules.Add(knowledgeRule);

                // Elimina la domanda non risposta
                _context.UnansweredQuestions.Remove(question);

                await _context.SaveChangesAsync();

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

        // Private helper methods
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

        private async Task<List<FileEntity>> GetFilesAllAsync(Guid userId)
        {
            return await _context.Files
                .AsNoTracking()
                .Where(f => EF.Property<Guid>(f, "UserId") == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }
    }
} 