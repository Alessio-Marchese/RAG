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
                return result.IsSuccessful
                    ? new OkObjectResult(result.Data)
                    : new ObjectResult(result.ErrorMessage) { StatusCode = 500 };
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex.Message);
            }
        }

        public async Task<IActionResult> RunAsync(Func<Task<Result>> action)
        {
            try
            {
                var result = await action();
                return result.IsSuccessful
                    ? new OkResult()
                    : new ObjectResult(result.ErrorMessage) { StatusCode = 500 };
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex.Message);
            }
        }
    }
} 