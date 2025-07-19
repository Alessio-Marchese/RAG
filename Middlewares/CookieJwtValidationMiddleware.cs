using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace RAG.Middlewares
{
    public class CookieJwtValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public CookieJwtValidationMiddleware(RequestDelegate next, TokenValidationParameters tokenValidationParameters)
        {
            _next = next;
            _tokenValidationParameters = tokenValidationParameters;
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
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token non valido o scaduto");
                    return;
                }
            }
            else
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token mancante");
                return;
            }

            await _next(context);
        }
    }
} 