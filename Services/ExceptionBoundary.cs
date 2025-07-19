using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace RAG.Services
{
    public static class ExceptionBoundary
    {
        public static async Task<IActionResult> RunAsync(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (ArgumentException ex)
            {
                return new BadRequestObjectResult(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return new UnauthorizedObjectResult(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                // Here you could log the error if you have a logging system
                return new ObjectResult(new { error = "Internal server error", details = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }
    }
} 