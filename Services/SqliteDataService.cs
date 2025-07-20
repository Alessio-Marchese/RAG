using Alessio.Marchese.Utils.Core;
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

        public async Task<Result<UserConfiguration>> GetUserConfigurationAsync(Guid userId)
        {
            var userConfig = await _context.UserConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (userConfig != null)
            {
                var knowledgeRulesResult = await GetKnowledgeRulesAllAsync(userId);
                if (!knowledgeRulesResult.IsSuccessful)
                    return knowledgeRulesResult.ToResult<UserConfiguration>();

                userConfig.KnowledgeRules = knowledgeRulesResult.Data;

                var filesResult = await GetFilesAllAsync(userId);
                if (!filesResult.IsSuccessful)
                    return filesResult.ToResult<UserConfiguration>();

                userConfig.Files = filesResult.Data;
            }

            return Result<UserConfiguration>.Success(userConfig);
        }

        public async Task<Result> UpdateUserConfigurationGranularAsync(
            Guid userId,
            List<KnowledgeRule>? knowledgeRulesToAdd,
            List<FileEntity>? filesToAdd,
            List<Guid>? knowledgeRulesToDelete,
            List<Guid>? filesToDelete)
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

            return Result.Success();
        }

        public async Task<Result<List<UnansweredQuestion>>> GetUnansweredQuestionsAsync()
        {
            var questions = await _context.UnansweredQuestions
                .AsNoTracking()
                .OrderByDescending(q => q.Timestamp)
                .ToListAsync();

            return Result<List<UnansweredQuestion>>.Success(questions);
        }

        public async Task<Result> DeleteUnansweredQuestionAsync(Guid questionId)
        {
            var question = await _context.UnansweredQuestions
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                return Result.Failure("Question not found");
            }

            _context.UnansweredQuestions.Remove(question);
            await _context.SaveChangesAsync();

            return Result.Success();
        }

        #region PRIVATE METHODS
        private async Task<Result<List<KnowledgeRule>>> GetKnowledgeRulesAllAsync(Guid userId)
        {
            var rules = await _context.KnowledgeRules
                .AsNoTracking()
                .Where(kr => EF.Property<Guid>(kr, "UserId") == userId)
                .ToListAsync();
            return Result<List<KnowledgeRule>>.Success(rules);
        }

        private async Task<Result<List<FileEntity>>> GetFilesAllAsync(Guid userId)
        {
            var files = await _context.Files
                .AsNoTracking()
                .Where(f => EF.Property<Guid>(f, "UserId") == userId)
                .ToListAsync();
            return Result<List<FileEntity>>.Success(files);
        }
        #endregion
    }
} 