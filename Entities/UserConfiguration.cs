using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace RAG.Entities
{
    public class UserConfiguration
    {
        [Required]
        public Guid UserId { get; set; } = Guid.NewGuid();
        public List<KnowledgeRule> KnowledgeRules { get; set; } = new();
        public List<ToneRule> ToneRules { get; set; } = new();
        public List<File> Files { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
} 