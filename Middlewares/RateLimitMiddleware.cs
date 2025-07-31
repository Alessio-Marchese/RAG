using RAG.Services;
using System.Security.Claims;
using Alessio.Marchese.Utils.Core;

namespace RAG.Middlewares
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRateLimitService _rateLimitService;

        public RateLimitMiddleware(RequestDelegate next, IRateLimitService rateLimitService)
        {
            _next = next;
            _rateLimitService = rateLimitService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { 
                    error = "User must be authenticated for rate limiting",
                    authentication = "Missing user ID in claims",
                    endpoint = context.Request.Path,
                    method = context.Request.Method
                });
                return;
            }
            
            var key = $"rate_limit_{userId}_{context.Request.Path}";
            
            var isAllowed = await _rateLimitService.IsAllowedAsync(key, 100, TimeSpan.FromMinutes(1));
            
            if (!isAllowed)
            {
                var remaining = await _rateLimitService.GetRemainingRequestsAsync(key, 100, TimeSpan.FromMinutes(1));
                context.Response.StatusCode = 429;
                context.Response.Headers.Append("X-RateLimit-Remaining", remaining.ToString());
                context.Response.Headers.Append("X-RateLimit-Reset", DateTime.UtcNow.AddMinutes(1).ToString("R"));
                await context.Response.WriteAsJsonAsync(new { 
                    error = $"Rate limit exceeded. Maximum 100 requests per minute allowed. Please wait before making additional requests.",
                    rateLimit = new {
                        limit = 100,
                        remaining = remaining,
                        resetTime = DateTime.UtcNow.AddMinutes(1).ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        window = "1 minute"
                    },
                    endpoint = context.Request.Path,
                    method = context.Request.Method,
                    userId = userId
                });
                return;
            }

            var remainingRequests = await _rateLimitService.GetRemainingRequestsAsync(key, 100, TimeSpan.FromMinutes(1));
            context.Response.Headers.Append("X-RateLimit-Remaining", remainingRequests.ToString());
            context.Response.Headers.Append("X-RateLimit-Limit", "100");

            await _next(context);
        }
    }
} 