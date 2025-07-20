using Microsoft.AspNetCore.Mvc;
using RAG.Services;
using RAG.Facades;

namespace RAG.Controllers
{
    [ApiController]
    [Route("api/unanswered-questions")]
    public class UnansweredQuestionsController : ControllerBase
    {
        private readonly IUnansweredQuestionsFacade _unansweredQuestionsFacade;
        private readonly IExceptionBoundary _exceptionBoundary;

        public UnansweredQuestionsController(IUnansweredQuestionsFacade unansweredQuestionsFacade, IExceptionBoundary exceptionBoundary)
        {
            _unansweredQuestionsFacade = unansweredQuestionsFacade;
            _exceptionBoundary = exceptionBoundary;
        }

        [HttpGet]
        public Task<IActionResult> GetUnansweredQuestions()
            => _exceptionBoundary.RunAsync(_unansweredQuestionsFacade.GetUnansweredQuestionsAsync);

        [HttpDelete("{questionId}")]
        public Task<IActionResult> DeleteUnansweredQuestion(Guid questionId)
            => _exceptionBoundary.RunAsync(() => _unansweredQuestionsFacade.DeleteUnansweredQuestionAsync(questionId));
    }
} 