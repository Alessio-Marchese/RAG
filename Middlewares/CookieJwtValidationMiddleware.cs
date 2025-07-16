using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace RAG.Middlewares
{
    public class CookieJwtValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly ILogger<CookieJwtValidationMiddleware> _logger;

        public CookieJwtValidationMiddleware(RequestDelegate next, TokenValidationParameters tokenValidationParameters, ILogger<CookieJwtValidationMiddleware> logger)
        {
            _next = next;
            _tokenValidationParameters = tokenValidationParameters;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Cookies.TryGetValue("app_token", out var token))
            {
                var handler = new JwtSecurityTokenHandler();
                try
                {
                    var principal = handler.ValidateToken(token, _tokenValidationParameters, out _);
                    context.User = principal;
                    var sub = principal.FindFirst("sub")?.Value;
                    var email = principal.FindFirst("email")?.Value;
                    var name = principal.FindFirst("name")?.Value;
                    var subscription = principal.FindFirst("subscription")?.Value;
                    _logger.LogInformation($"[CookieJwt] Token valido. sub: {sub}, email: {email}, name: {name}, subscription: {subscription}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[CookieJwt] Token non valido: {ex.Message}");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token non valido o scaduto");
                    return;
                }
            }
            else
            {
                _logger.LogWarning("[CookieJwt] Cookie app_token mancante");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token mancante");
                return;
            }

            await _next(context);
        }
    }
} 