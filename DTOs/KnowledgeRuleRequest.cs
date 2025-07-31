using System.ComponentModel.DataAnnotations;

namespace RAG.DTOs
{
    public class KnowledgeRuleRequest
    {        
        [Required(ErrorMessage = "Knowledge rule content is required and cannot be empty")]
        [StringLength(10000, ErrorMessage = "Knowledge rule content cannot exceed 10000 characters")]
        public string Content { get; set; } = string.Empty;
    }
} 