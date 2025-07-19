using Microsoft.EntityFrameworkCore;
using RAG.Data;
using RAG.Entities;
using FileEntity = RAG.Entities.File;

namespace RAG.Services
{
    public class SqliteDataService
    {
        private readonly ApplicationDbContext _context;

        public SqliteDataService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserConfiguration?> GetUserConfigurationAsync(Guid userId)
        {
            try
            {
                var userConfig = await _context.UserConfigurations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (userConfig != null)
                {
                    userConfig.KnowledgeRules = await GetKnowledgeRulesAllAsync(userId);
                    userConfig.Files = await GetFilesAllAsync(userId);
                }

                return userConfig;
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore durante il recupero della configurazione utente per l'utente {userId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateUserConfigurationGranularAsync(Guid userId, 
            List<KnowledgeRule>? knowledgeRulesToAdd,
            List<FileEntity>? filesToAdd,
            List<Guid>? knowledgeRulesToDelete,
            List<Guid>? filesToDelete)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var existingConfig = await _context.UserConfigurations
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (existingConfig == null)
                {
                    var newConfig = new UserConfiguration
                    {
                        UserId = userId
                    };
                    _context.UserConfigurations.Add(newConfig);
                }

                if (knowledgeRulesToDelete == null)
                {
                    var allRulesToRemove = await _context.KnowledgeRules
                        .Where(kr => EF.Property<Guid>(kr, "UserId") == userId)
                        .ToListAsync();
                    _context.KnowledgeRules.RemoveRange(allRulesToRemove);
                }
                else if (knowledgeRulesToDelete.Any())
                {
                    var rulesToRemove = await _context.KnowledgeRules
                        .Where(kr => knowledgeRulesToDelete.Contains(kr.Id) && 
                                     EF.Property<Guid>(kr, "UserId") == userId)
                        .ToListAsync();
                    _context.KnowledgeRules.RemoveRange(rulesToRemove);
                }

                if (filesToDelete == null)
                {
                    var allFilesToRemove = await _context.Files
                        .Where(f => EF.Property<Guid>(f, "UserId") == userId)
                        .ToListAsync();
                    _context.Files.RemoveRange(allFilesToRemove);
                }
                else if (filesToDelete.Any())
                {
                    var filesToRemove = await _context.Files
                        .Where(f => filesToDelete.Contains(f.Id) && 
                                    EF.Property<Guid>(f, "UserId") == userId)
                        .ToListAsync();
                    _context.Files.RemoveRange(filesToRemove);
                }

                if (knowledgeRulesToAdd?.Any() == true)
                {
                    foreach (var kr in knowledgeRulesToAdd)
                    {
                        _context.Entry(kr).Property("UserId").CurrentValue = userId;
                        _context.KnowledgeRules.Add(kr);
                    }
                }

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

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore durante l'aggiornamento della configurazione utente: {ex.Message}", ex);
            }
        }

        public async Task<List<UnansweredQuestion>> GetUnansweredQuestionsAsync()
        {
            try
            {
                var questions = await _context.UnansweredQuestions
                    .AsNoTracking()
                    .OrderByDescending(q => q.Timestamp)
                    .ToListAsync();

                return questions;
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore durante il recupero delle domande non risposte: {ex.Message}", ex);
            }
        }



        public async Task<bool> DeleteUnansweredQuestionAsync(Guid questionId)
        {
            try
            {
                var question = await _context.UnansweredQuestions
                    .FirstOrDefaultAsync(q => q.Id == questionId);

                if (question == null)
                {
                    return false;
                }

                _context.UnansweredQuestions.Remove(question);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore durante l'eliminazione della domanda non risposta: {ex.Message}", ex);
            }
        }

#region PRIVATE METHODS
        private async Task<List<KnowledgeRule>> GetKnowledgeRulesAllAsync(Guid userId)
        {
            try
            {
                return await _context.KnowledgeRules
                    .AsNoTracking()
                    .Where(kr => EF.Property<Guid>(kr, "UserId") == userId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore durante il recupero delle knowledge rules per l'utente {userId}: {ex.Message}", ex);
            }
        }

        private async Task<List<FileEntity>> GetFilesAllAsync(Guid userId)
        {
            try
            {
                return await _context.Files
                    .AsNoTracking()
                    .Where(f => EF.Property<Guid>(f, "UserId") == userId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore durante il recupero dei file per l'utente {userId}: {ex.Message}", ex);
            }
        }
#endregion
    }
} 