using System.Security.Claims;
using Alessio.Marchese.Utils.Core;

namespace RAG.Services
{
    public interface ISessionService
    {
        Result<Guid> GetCurrentUserId();
    }
    public class SessionService : ISessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public SessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Result<Guid> GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                return Result<Guid>.Failure("User context not available. Please ensure you are authenticated.");
            
            var userIdString = user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            if (string.IsNullOrEmpty(userIdString))
                return Result<Guid>.Failure("User ID not found in claims. Please ensure you are properly authenticated.");
            
            if (!Guid.TryParse(userIdString, out var userId))
                return Result<Guid>.Failure("Invalid user ID format in authentication token.");
            
            return Result<Guid>.Success(userId);
        }
    }
} 