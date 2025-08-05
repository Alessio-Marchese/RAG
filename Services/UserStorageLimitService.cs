using RAG.DTOs;
using RAG.Entities;
using RAG.Services;
using Alessio.Marchese.Utils.Core;

namespace RAG.Services
{
    public interface IUserStorageLimitService
    {
        Task<Result> ValidateStorageLimitAsync(Guid userId, UpdateUserConfigurationRequest request);
    }

    public class UserStorageLimitService : IUserStorageLimitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private const long MAX_STORAGE_SIZE_BYTES = 10 * 1024 * 1024;

        public UserStorageLimitService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> ValidateStorageLimitAsync(Guid userId, UpdateUserConfigurationRequest request)
        {
            var currentSize = await GetCurrentUserStorageSizeAsync(userId);
            var newFilesSize = CalculateNewFilesSize(request.Files);
            var newKnowledgeRulesSize = CalculateKnowledgeRulesSize(request.KnowledgeRules);
            
            var filesToDeleteSize = await CalculateFilesToDeleteSizeAsync(userId, request.FilesToDelete);
            var knowledgeRulesToDeleteSize = await CalculateKnowledgeRulesToDeleteSizeAsync(userId, request.KnowledgeRulesToDelete);

            var totalSize = currentSize + newFilesSize + newKnowledgeRulesSize - filesToDeleteSize - knowledgeRulesToDeleteSize;

            if (totalSize > MAX_STORAGE_SIZE_BYTES)
            {
                var currentSizeMB = Math.Round((double)currentSize / (1024 * 1024), 2);
                var totalSizeMB = Math.Round((double)totalSize / (1024 * 1024), 2);
                return Result.Failure($"Storage limit exceeded. Current usage: {currentSizeMB}MB, Total after operation: {totalSizeMB}MB. Maximum allowed: 10MB.");
            }

            return Result.Success();
        }

        private async Task<long> GetCurrentUserStorageSizeAsync(Guid userId)
        {
            var files = await _unitOfWork.Files.GetByUserIdAsync(userId);
            var knowledgeRules = await _unitOfWork.KnowledgeRules.GetByUserIdAsync(userId);

            var filesSize = files.Sum(f => f.Size);
            var knowledgeRulesSize = CalculateKnowledgeRulesSize(knowledgeRules.Select(kr => new KnowledgeRuleRequest { Content = kr.Content }).ToList());

            return filesSize + knowledgeRulesSize;
        }

        private long CalculateNewFilesSize(List<FileRequest>? files)
        {
            if (files == null || !files.Any())
                return 0;

            return files.Sum(f => f.Size);
        }

        private long CalculateKnowledgeRulesSize(List<KnowledgeRuleRequest>? knowledgeRules)
        {
            if (knowledgeRules == null || !knowledgeRules.Any())
                return 0;

            return knowledgeRules.Sum(kr => System.Text.Encoding.UTF8.GetByteCount(kr.Content));
        }

        private async Task<long> CalculateFilesToDeleteSizeAsync(Guid userId, List<Guid>? filesToDelete)
        {
            if (filesToDelete == null || !filesToDelete.Any())
                return 0;

            var files = await _unitOfWork.Files.GetByIdsAsync(filesToDelete);
            return files.Sum(f => f.Size);
        }

        private async Task<long> CalculateKnowledgeRulesToDeleteSizeAsync(Guid userId, List<Guid>? knowledgeRulesToDelete)
        {
            if (knowledgeRulesToDelete == null || !knowledgeRulesToDelete.Any())
                return 0;

            var knowledgeRules = await _unitOfWork.KnowledgeRules.GetByIdsAsync(knowledgeRulesToDelete);
            return knowledgeRules.Sum(kr => System.Text.Encoding.UTF8.GetByteCount(kr.Content));
        }
    }
} 