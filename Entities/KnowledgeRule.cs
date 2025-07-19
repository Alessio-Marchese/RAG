using System.ComponentModel.DataAnnotations;

namespace RAG.Entities
{
    public class KnowledgeRule
    {
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Content { get; set; } = string.Empty;
    }
} 