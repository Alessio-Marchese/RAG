using Microsoft.AspNetCore.Mvc;
using Alessio.Marchese.Utils.Core;

namespace RAG.Services
{
    public interface IExceptionBoundary
    {
        Task<IActionResult> RunAsync<T>(Func<Task<Result<T>>> action);
        Task<IActionResult> RunAsync(Func<Task<Result>> action);
    }

    public class ExceptionBoundary : IExceptionBoundary
    {
        public async Task<IActionResult> RunAsync<T>(Func<Task<Result<T>>> action)
        {
            try
            {
                var result = await action();
                if (result.IsSuccessful)
                    return new OkObjectResult(result.Data);
                
                return new ObjectResult(new { 
                    error = result.ErrorMessage ?? "Operation failed due to validation or business logic error",
                    operation = "Data retrieval operation"
                }) { StatusCode = 400 };
            }
            catch (Exception)
            {
                return new ObjectResult(new { 
                    error = "Internal server error occurred during data retrieval operation. Please contact system administrator.",
                    operation = "Data retrieval operation",
                    timestamp = DateTime.UtcNow
                }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> RunAsync(Func<Task<Result>> action)
        {
            try
            {
                var result = await action();
                if (result.IsSuccessful)
                    return new OkResult();
                
                return new ObjectResult(new { 
                    error = result.ErrorMessage ?? "Operation failed due to validation or business logic error",
                    operation = "Data modification operation"
                }) { StatusCode = 400 };
            }
            catch (Exception)
            {
                return new ObjectResult(new { 
                    error = "Internal server error occurred during data modification operation. Please contact system administrator.",
                    operation = "Data modification operation",
                    timestamp = DateTime.UtcNow
                }) { StatusCode = 500 };
            }
        }
    }
} 