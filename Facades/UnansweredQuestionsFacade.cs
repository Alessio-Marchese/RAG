using RAG.Services;
using Alessio.Marchese.Utils.Core;
using RAG.Entities;

namespace RAG.Facades
{
    public interface IUnansweredQuestionsFacade
    {
        Task<Result<List<UnansweredQuestion>>> GetUnansweredQuestionsAsync();
        Task<Result> DeleteUnansweredQuestionAsync(Guid questionId);
    }

    public class UnansweredQuestionsFacade : IUnansweredQuestionsFacade
    {
        private readonly SqliteDataService _dataService;

        public UnansweredQuestionsFacade(SqliteDataService dataService)
        {
            _dataService = dataService;
        }

        public async Task<Result<List<UnansweredQuestion>>> GetUnansweredQuestionsAsync()
        {
            var result = await _dataService.GetUnansweredQuestionsAsync();
            
            if (!result.IsSuccessful)
                return result.ToResult<List<UnansweredQuestion>>();

            return Result<List<UnansweredQuestion>>.Success(result.Data);
        }

        public async Task<Result> DeleteUnansweredQuestionAsync(Guid questionId)
        {
            var result = await _dataService.DeleteUnansweredQuestionAsync(questionId);
                
            if (!result.IsSuccessful)
                return result.ToResult();

            return result;
        }
    }
} 