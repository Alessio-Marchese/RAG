using System.ComponentModel.DataAnnotations;

namespace RAG.Entities
{
    public class AnswerQuestionRequest
    {
        [Required]
        public string Answer { get; set; } = string.Empty;
        [Required]
        public Guid UserId { get; set; }
    }
} 