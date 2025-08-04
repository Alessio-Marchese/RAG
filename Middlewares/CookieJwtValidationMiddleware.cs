using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Alessio.Marchese.Utils.Core;
using System.Security.Claims;

namespace RAG.Middlewares
{
    public class CookieJwtValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public CookieJwtValidationMiddleware(
            RequestDelegate next, 
            TokenValidationParameters tokenValidationParameters)
        {
            _next = next;
            _tokenValidationParameters = tokenValidationParameters;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var validationResult = ValidateTokenAsync(context);
            
            if (!validationResult.IsSuccessful)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { 
                    error = validationResult.ErrorMessage,
                    authentication = "JWT token validation failed",
                    endpoint = context.Request.Path,
                    method = context.Request.Method
                });
                return;
            }

            await _next(context);
        }

        private Result ValidateTokenAsync(HttpContext context)
        {
            try
            {
                if (!context.Request.Cookies.TryGetValue("app_token", out var token))
                    return Result.Failure("Authentication token missing from cookies. Please log in again.");

                if (string.IsNullOrWhiteSpace(token))
                    return Result.Failure("Authentication token is empty or invalid. Please log in again.");

                var handler = new JwtSecurityTokenHandler();
                
                var principal = handler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
                
                if (validatedToken == null)
                    return Result.Failure("Authentication token validation failed. Token structure is invalid.");

                if (validatedToken.ValidTo < DateTime.UtcNow)
                    return Result.Failure($"Authentication token has expired. Token expired at {validatedToken.ValidTo:yyyy-MM-dd HH:mm:ss} UTC. Please log in again.");

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                var name = principal.FindFirst("name")?.Value;
                var subscription = principal.FindFirst("subscription")?.Value;
                var termsAccepted = principal.FindFirst("termsAccepted")?.Value;
                var privacyAccepted = principal.FindFirst("privacyAccepted")?.Value;

                if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out _))
                    return Result.Failure("Authentication token is invalid or corrupted. Please log in again.");

                if (string.IsNullOrWhiteSpace(termsAccepted) || !bool.TryParse(termsAccepted, out var termsAcceptedBool) || !termsAcceptedBool)
                    return Result.Failure("Terms and conditions must be accepted to access this service. Please accept the terms and conditions.");

                if (string.IsNullOrWhiteSpace(privacyAccepted) || !bool.TryParse(privacyAccepted, out var privacyAcceptedBool) || !privacyAcceptedBool)
                    return Result.Failure("Privacy policy must be accepted to access this service. Please accept the privacy policy.");

                context.User = principal;
                return Result.Success();
            }
            catch (SecurityTokenExpiredException)
            {
                return Result.Failure("Authentication token has expired. Please log in again.");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return Result.Failure("Authentication token signature is invalid. Please log in again.");
            }
            catch (SecurityTokenInvalidIssuerException)
            {
                return Result.Failure("Authentication token issuer is invalid. Please log in again.");
            }
            catch (SecurityTokenInvalidAudienceException)
            {
                return Result.Failure("Authentication token audience is invalid. Please log in again.");
            }
            catch (SecurityTokenNotYetValidException)
            {
                return Result.Failure("Authentication token is not yet valid. Please check your system clock.");
            }
            catch (SecurityTokenException)
            {
                return Result.Failure("Authentication token validation failed. Please log in again.");
            }
            catch (Exception)
            {
                return Result.Failure("Internal authentication error. Please try again or contact support.");
            }
        }
    }
}
