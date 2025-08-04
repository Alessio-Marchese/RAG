using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAG.DTOs;
using RAG.Facades;
using RAG.Services;

namespace RAG.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUsersFacade _usersFacade;
        private readonly IExceptionBoundary _exceptionBoundary;

        public UsersController(IUsersFacade usersFacade, IExceptionBoundary exceptionBoundary)
        {
            _usersFacade = usersFacade;
            _exceptionBoundary = exceptionBoundary;
        }

        [HttpGet("configuration/paginated")]
        public Task<IActionResult> GetUserConfigurationPaginated([FromQuery] int skip = 0, [FromQuery] int take = 10)
        {
            if (take <= 0 || take > 100)
            {
                return Task.FromResult<IActionResult>(
                    new BadRequestObjectResult(new { 
                        error = "Take parameter must be between 1 and 100",
                        operation = "Get user configuration paginated",
                        endpoint = "/api/Users/configuration/paginated",
                        method = "GET"
                    }));
            }
            
            return _exceptionBoundary.RunAsync(() => _usersFacade.GetUserConfigurationPaginatedAsync(skip, take));
        }

        [HttpPut("configuration")]
        public Task<IActionResult> UpdateUserConfiguration([FromBody] UpdateUserConfigurationRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return Task.FromResult<IActionResult>(
                    new BadRequestObjectResult(new { 
                        error = "Request validation failed. Please check the provided data and try again.",
                        validationErrors = errors,
                        operation = "Update user configuration",
                        endpoint = "/api/Users/configuration",
                        method = "PUT"
                    }));
            }
            
            return _exceptionBoundary.RunAsync(() => _usersFacade.UpdateUserConfigurationAsync(request));
        }

        [HttpGet("storage/usage")]
        public Task<IActionResult> GetUserStorageUsage()
        {
            return _exceptionBoundary.RunAsync(() => _usersFacade.GetUserStorageUsageAsync());
        }
    }
} 