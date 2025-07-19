using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace RAG.Entities
{
    public class UserConfiguration
    {
        [Required]
        public Guid UserId { get; set; } = Guid.NewGuid();
        public List<KnowledgeRule> KnowledgeRules { get; set; } = [];
        public List<File> Files { get; set; } = [];
        public bool IsProcessing { get; set; } = false;
    }
} 