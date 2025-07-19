using Microsoft.AspNetCore.Mvc;
using RAG.Entities;
using RAG.Services;

namespace RAG.Controllers
{
    [ApiController]
    [Route("api/unanswered-questions")]
    public class UnansweredQuestionsController : ControllerBase
    {
        private readonly SqliteDataService _dataService;

        public UnansweredQuestionsController(SqliteDataService dataService)
        {
            _dataService = dataService;
        }

        [HttpGet]
        public Task<IActionResult> GetUnansweredQuestions()
        {
            return ExceptionBoundary.RunAsync(async () =>
            {
                var questions = await _dataService.GetUnansweredQuestionsAsync();
                
                var response = questions.Select(q => new
                {
                    id = q.Id,
                    question = q.Question,
                    timestamp = q.Timestamp
                }).ToList();

                return Ok(response);
            });
        }

        [HttpDelete("{questionId}")]
        public Task<IActionResult> DeleteUnansweredQuestion(Guid questionId)
        {
            return ExceptionBoundary.RunAsync(async () =>
            {
                var success = await _dataService.DeleteUnansweredQuestionAsync(questionId);
                
                if (success)
                {
                    return Ok(new SuccessResponse { Message = "Question deleted successfully" });
                }
                else
                {
                    return NotFound(new ErrorResponse { Message = "Question not found" });
                }
            });
        }
    }
} 