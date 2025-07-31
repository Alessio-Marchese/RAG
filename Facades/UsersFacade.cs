using RAG.DTOs;
using RAG.Services;
using RAG.Mappers;
using Alessio.Marchese.Utils.Core;

namespace RAG.Facades
{
    public interface IUsersFacade
    {
        Task<Result<UserConfigurationResponse>> GetUserConfigurationPaginatedAsync(int skip, int take);
        Task<Result> UpdateUserConfigurationAsync(UpdateUserConfigurationRequest request);
    }
    
    public class UsersFacade : IUsersFacade
    {
        private readonly IUserConfigurationService _userConfigurationService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ISessionService _sessionService;

        public UsersFacade(
            IUserConfigurationService userConfigurationService,
            IFileStorageService fileStorageService,
            ISessionService sessionService)
        {
            _userConfigurationService = userConfigurationService;
            _fileStorageService = fileStorageService;
            _sessionService = sessionService;
        }

        public async Task<Result<UserConfigurationResponse>> GetUserConfigurationPaginatedAsync(int skip, int take)
        {
            var userIdResult = _sessionService.GetCurrentUserId();
            if (!userIdResult.IsSuccessful)
                return Result<UserConfigurationResponse>.Failure(userIdResult.ErrorMessage);

            return await _userConfigurationService.GetUserConfigurationPaginatedAsync(userIdResult.Data, skip, take);
        }

        public async Task<Result> UpdateUserConfigurationAsync(UpdateUserConfigurationRequest request)
        {
            var userIdResult = _sessionService.GetCurrentUserId();
            if (!userIdResult.IsSuccessful)
                return Result.Failure(userIdResult.ErrorMessage);
            
            if (request == null)
                return Result.Failure("Update request is null. Please provide valid configuration data.");

            var userId = userIdResult.Data;

            if (request.FilesToDelete?.Any() == true)
            {
                var deleteResult = await _fileStorageService.DeleteFilesAsync(userId, request.FilesToDelete);
                if (!deleteResult.IsSuccessful)
                    return deleteResult.ToResult();
            }

            if (request.KnowledgeRulesToDelete?.Any() == true)
            {
                var deleteEmbeddingsResult = await _fileStorageService.UpdateKnowledgeRulesFileAsync(userId, []);
                if (!deleteEmbeddingsResult.IsSuccessful)
                    return deleteEmbeddingsResult.ToResult();
            }

            var updateResult = await _userConfigurationService.UpdateUserConfigurationAsync(userId, request);
            if (!updateResult.IsSuccessful)
                return updateResult.ToResult();

            if (request.Files?.Any() == true)
            {
                var uploadResult = await _fileStorageService.UploadFilesAsync(userId, request.Files);
                if (!uploadResult.IsSuccessful)
                    return uploadResult.ToResult();
            }

            if (request.KnowledgeRules?.Any() == true || request.KnowledgeRulesToDelete?.Any() == true)
            {
                var configResult = await _userConfigurationService.GetUserConfigurationAsync(userId);
                if (!configResult.IsSuccessful)
                    return configResult.ToResult();

                var remainingKnowledgeRules = configResult.Data?.KnowledgeRules?.Select(kr => kr.ToEntity()).ToList() ?? [];

                var updateKnowledgeRulesResult = await _fileStorageService.UpdateKnowledgeRulesFileAsync(userId, remainingKnowledgeRules);
                if (!updateKnowledgeRulesResult.IsSuccessful)
                    return updateKnowledgeRulesResult.ToResult();
            }

            return Result.Success();
        }
    }
}
