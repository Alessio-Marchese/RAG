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

        [HttpGet("configuration")]
        public Task<IActionResult> GetUserConfiguration()
            => _exceptionBoundary.RunAsync(_usersFacade.GetUserConfigurationAsync);

        [HttpPut("configuration")]
        public Task<IActionResult> UpdateUserConfiguration([FromBody] UpdateUserConfigurationRequest request)
            => _exceptionBoundary.RunAsync(() => _usersFacade.UpdateUserConfigurationAsync(request));
    }
} 