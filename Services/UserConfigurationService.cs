using Microsoft.EntityFrameworkCore;
using RAG.DTOs;
using RAG.Mappers;
using Alessio.Marchese.Utils.Core;

namespace RAG.Services
{
    public interface IUserConfigurationService
    {
        Task<Result<UserConfigurationResponse>> GetUserConfigurationAsync(Guid userId);
        Task<Result<UserConfigurationResponse>> GetUserConfigurationPaginatedAsync(Guid userId, int skip, int take);
        Task<Result> UpdateUserConfigurationAsync(Guid userId, UpdateUserConfigurationRequest request);
    }

    public class UserConfigurationService : IUserConfigurationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IFileValidationService _fileValidationService;

        public UserConfigurationService(
            IUnitOfWork unitOfWork, 
            ICacheService cacheService,
            IFileValidationService fileValidationService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _fileValidationService = fileValidationService;
        }

        public async Task<Result<UserConfigurationResponse>> GetUserConfigurationAsync(Guid userId)
        {
            var cachedConfig = await _cacheService.GetAsync<UserConfigurationResponse>($"user_config_{userId}");
            if (cachedConfig != null)
                return Result<UserConfigurationResponse>.Success(cachedConfig);

            var knowledgeRules = await _unitOfWork.KnowledgeRules.GetByUserIdAsync(userId);
            var files = await _unitOfWork.Files.GetByUserIdAsync(userId);

            var response = knowledgeRules.ToUserConfigurationResponse(files);
            
            await _cacheService.SetAsync($"user_config_{userId}", response, TimeSpan.FromMinutes(5));
            
            return Result<UserConfigurationResponse>.Success(response);
        }

        public async Task<Result<UserConfigurationResponse>> GetUserConfigurationPaginatedAsync(Guid userId, int skip, int take)
        {
            var knowledgeRules = await _unitOfWork.KnowledgeRules.GetByUserIdPaginatedAsync(userId, skip, take);
            var files = await _unitOfWork.Files.GetByUserIdPaginatedAsync(userId, skip, take);

            var response = knowledgeRules.ToUserConfigurationResponse(files);
            
            return Result<UserConfigurationResponse>.Success(response);
        }

        public async Task<Result> UpdateUserConfigurationAsync(Guid userId, UpdateUserConfigurationRequest request)
        {
            if (request == null)
                return Result.Failure("Update request is null. Please provide valid configuration data.");

            if (request.Files?.Any() == true)
            {
                foreach (var file in request.Files)
                {
                    var validationResult = await _fileValidationService.ValidateFileAsync(
                        file.Name, file.ContentType, file.Size);
                    
                    if (!validationResult.IsSuccessful)
                        return validationResult.ToResult();
                }
            }

            var operations = new List<Func<Task<Result>>>();

            if (request.Files?.Any() == true)
                operations.Add(() => CheckForDuplicateFilesAsync(userId, request.Files));

            operations.Add(() => UpdateConfigurationDataAsync(userId, request));

            var result = await _unitOfWork.ExecuteTransactionAsync(operations.ToArray());
            
            if (result.IsSuccessful)
                await _cacheService.RemoveAsync($"user_config_{userId}");

            return result;
        }
#region PRIVATE METHODS
        private async Task<Result> CheckForDuplicateFilesAsync(Guid userId, List<FileRequest> filesToAdd)
        {
            var existingFileNames = await _unitOfWork.Files.GetFileNamesByUserIdAsync(userId);
            var duplicateFiles = filesToAdd
                .Where(f => existingFileNames.Contains(f.Name))
                .Select(f => f.Name)
                .ToList();

            if (duplicateFiles.Any())
                return Result.Failure($"Duplicate files found: {string.Join(", ", duplicateFiles)}. Please use different file names or delete existing files first.");

            return Result.Success();
        }

        private async Task<Result> UpdateConfigurationDataAsync(Guid userId, UpdateUserConfigurationRequest request)
        {
            if (request.KnowledgeRulesToDelete?.Any() == true)
            {
                var success = await _unitOfWork.KnowledgeRules.DeleteMultipleAsync(request.KnowledgeRulesToDelete, userId);
                if (!success)
                    return Result.Failure($"Failed to delete knowledge rules for user {userId}");
            }

            if (request.FilesToDelete?.Any() == true)
            {
                var success = await _unitOfWork.Files.DeleteMultipleAsync(request.FilesToDelete, userId);
                if (!success)
                    return Result.Failure($"Failed to delete files for user {userId}");
            }

            if (request.KnowledgeRules?.Any() == true)
                foreach (var ruleRequest in request.KnowledgeRules)
                {
                    var knowledgeRule = ruleRequest.ToEntity();
                    knowledgeRule.UserId = userId;
                    await _unitOfWork.KnowledgeRules.CreateAsync(knowledgeRule);
                }

            if (request.Files?.Any() == true)
                foreach (var fileRequest in request.Files)
                {
                    var file = fileRequest.ToEntity();
                    file.UserId = userId;
                    await _unitOfWork.Files.CreateAsync(file);
                }

            return await _unitOfWork.SaveChangesAsync();
        }
#endregion
    }
}