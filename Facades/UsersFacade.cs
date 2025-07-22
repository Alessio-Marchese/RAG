using RAG.DTOs;
using RAG.Services;
using RAG.Entities;
using System.Text;
using Alessio.Marchese.Utils.Core;

namespace RAG.Facades
{

    public interface IUsersFacade
    {
        Task<Result<UserConfigurationResponse>> GetUserConfigurationAsync();
        Task<Result> UpdateUserConfigurationAsync(UpdateUserConfigurationRequest request);
    }
    public class UsersFacade : IUsersFacade
    {
        private readonly SqliteDataService _dataService;
        private readonly IPineconeService _pineconeService;
        private readonly IUserConfigService _userConfigService;
        private readonly IS3StorageService _storageService;
        private readonly ISessionService _sessionService;

        public UsersFacade(SqliteDataService dataService, IPineconeService pineconeService, IUserConfigService userConfigService, IS3StorageService storageService, ISessionService sessionService)
        {
            _dataService = dataService;
            _pineconeService = pineconeService;
            _userConfigService = userConfigService;
            _storageService = storageService;
            _sessionService = sessionService;
        }

        public async Task<Result<UserConfigurationResponse>> GetUserConfigurationAsync()
        {
                var userId = _sessionService.GetCurrentUserId();
                if (!userId.HasValue)
                    return Result<UserConfigurationResponse>.Failure("User not authenticated");

                var configurationResult = await _dataService.GetUserConfigurationAsync(userId.Value);
                
                if (!configurationResult.IsSuccessful)
                    return configurationResult.ToResult<UserConfigurationResponse>();

                var configuration = configurationResult.Data;
                
                if (configuration == null)
                {
                    configuration = new UserConfiguration
                    {
                        UserId = userId.Value,
                        KnowledgeRules = [],
                        Files = []
                    };
                    
                    var updateResult = await _dataService.UpdateUserConfigurationGranularAsync(userId.Value, 
                        [], 
                        [],
                        null, null);
                    
                    if (!updateResult.IsSuccessful)
                    {
                        return updateResult.ToResult<UserConfigurationResponse>();
                    }
                }

                var response = new UserConfigurationResponse
                {
                    KnowledgeRules = configuration.KnowledgeRules.Select(kr => new KnowledgeRuleResponse
                    {
                        Content = kr.Content
                    }).ToList(),
                    Files = configuration.Files.Select(f => new FileResponse
                    {
                        Id = f.Id,
                        Name = f.Name,
                        ContentType = f.ContentType,
                        Size = f.Size,
                        Content = f.Content
                    }).ToList()
                };

                return Result<UserConfigurationResponse>.Success(response);
        }

        public async Task<Result> UpdateUserConfigurationAsync(UpdateUserConfigurationRequest request)
        {
                var userId = _sessionService.GetCurrentUserId();
                if (!userId.HasValue)
                    return Result.Failure("User not authenticated");

                if (request == null)
                    return Result.Failure("Request cannot be null");

                var pineconeNamespace = userId.Value.ToString();
                await _pineconeService.DeleteAllEmbeddingsInNamespaceAsync(pineconeNamespace);

                var knowledgeRulesToAdd = request.KnowledgeRules?.Select(kr => new KnowledgeRule
                {
                    Id = kr.Id ?? Guid.NewGuid(),
                    Content = kr.Content
                }).ToList() ?? [];

                var filesToAdd = request.Files?.Select(f => new RAG.Entities.File
                {
                    Name = f.Name,
                    ContentType = f.ContentType,
                    Size = f.Size,
                    Content = f.Content
                }).ToList() ?? [];

                var updateResult = await _dataService.UpdateUserConfigurationGranularAsync(userId.Value,
                    knowledgeRulesToAdd,
                    filesToAdd,
                    null,
                    null
                );

                if (!updateResult.IsSuccessful)
                    return updateResult.ToResult();

                var allInfo = _userConfigService.SerializeUserConfigForS3(
                    request.KnowledgeRules ?? [],
                    request.Files ?? []
                );
                
                var fileNameToUpload = "user_config.txt";
                var fileBytes = Encoding.UTF8.GetBytes(allInfo);
                using var stream = new MemoryStream(fileBytes);

                await _storageService.DeleteAllUserFilesAsync(userId.Value.ToString());
                await _storageService.UploadFileAsync(userId.Value.ToString(), new FormFile(stream, 0, fileBytes.Length, "file", fileNameToUpload)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "text/plain"
                }, fileNameToUpload);

                return Result.Success();
        }
    }
}
