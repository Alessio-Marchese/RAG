using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAG.Entities;
using RAG.Services;
using FileEntity = RAG.Entities.File;
using RAG.DTOs;
using System.Text;

namespace RAG.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly SqliteDataService _dataService;
        private readonly IS3StorageService _storageService;
        private readonly IPineconeService _pineconeService;
        private readonly IUserConfigService _userConfigService;

        public UsersController(SqliteDataService dataService, IS3StorageService storageService, IPineconeService pineconeService, IUserConfigService userConfigService)
        {
            _dataService = dataService;
            _storageService = storageService;
            _pineconeService = pineconeService;
            _userConfigService = userConfigService;
        }

        [HttpGet("{userId}/configuration")]
        public Task<IActionResult> GetUserConfiguration(Guid userId)
        {
            return ExceptionBoundary.RunAsync(async () =>
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid();
                }

                var configuration = await _dataService.GetUserConfigurationAsync(userId);
                
                if (configuration == null)
                {
                    configuration = new UserConfiguration
                    {
                        UserId = userId,
                        KnowledgeRules = [],
                        Files = []
                    };
                    
                    await _dataService.UpdateUserConfigurationGranularAsync(userId, 
                        [], 
                        [],
                        null, null);
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

                return Ok(response);
            });
        }

        [HttpPut("{userId}/configuration")]
        public Task<IActionResult> UpdateUserConfiguration(Guid userId, [FromBody] UpdateUserConfigurationRequest request)
        {
            return ExceptionBoundary.RunAsync(async () =>
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    return Forbid();
                }

                if (request == null)
                {
                    return BadRequest(new ErrorResponse { Message = "Invalid request" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(new ErrorResponse 
                    { 
                        Message = "Invalid data", 
                        Details = string.Join(", ", errors) 
                    });
                }

                var pineconeNamespace = userId.ToString();
                await _pineconeService.DeleteAllEmbeddingsInNamespaceAsync(pineconeNamespace);

                var knowledgeRulesToAdd = request.KnowledgeRules?.Select(kr => new KnowledgeRule
                {
                    Id = kr.Id ?? Guid.NewGuid(),
                    Content = kr.Content
                }).ToList() ?? [];

                var filesToAdd = request.Files?.Select(f => new FileEntity
                {
                    Id = f.Id ?? Guid.NewGuid(),
                    Name = f.Name,
                    ContentType = f.ContentType,
                    Size = f.Size,
                    Content = f.Content
                }).ToList() ?? [];

                var success = await _dataService.UpdateUserConfigurationGranularAsync(userId,
                    knowledgeRulesToAdd,
                    filesToAdd,
                    null,
                    null
                );
                
                if (success)
                {
                    var allInfo = _userConfigService.SerializeUserConfigForS3(
                        request.KnowledgeRules ?? [],
                        request.Files ?? []
                    );
                    
                    var fileNameToUpload = "user_config.txt";
                    var fileBytes = Encoding.UTF8.GetBytes(allInfo);
                    using var stream = new MemoryStream(fileBytes);

                    await _storageService.DeleteAllUserFilesAsync(userId.ToString());

                    await _storageService.UploadFileAsync(userId.ToString(), new FormFile(stream, 0, fileBytes.Length, "file", fileNameToUpload)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "text/plain"
                    }, fileNameToUpload);

                    return Ok(new SuccessResponse { Message = "Configuration updated successfully" });
                }
                else
                {
                    return StatusCode(500, new ErrorResponse { Message = "Something went wrong during the update of the configuration." });
                }
            });
        }

#region PRIVATE METHODS
        private Guid GetCurrentUserId()
        {
            var userIdString = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Guid.Empty;
            }
            
            return userId;
        }
#endregion
    }
} 