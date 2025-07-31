using System.ComponentModel.DataAnnotations;

namespace RAG.DTOs
{
    public class UpdateUserConfigurationRequest
    {
        [MaxLength(100, ErrorMessage = "Maximum number of knowledge rules allowed is 100")]
        public List<KnowledgeRuleRequest>? KnowledgeRules { get; set; }
        
        [MaxLength(50, ErrorMessage = "Maximum number of files allowed is 50")]
        public List<FileRequest>? Files { get; set; }
        
        [MaxLength(100, ErrorMessage = "Maximum number of knowledge rules to delete allowed is 100")]
        public List<Guid>? KnowledgeRulesToDelete { get; set; }
        
        [MaxLength(50, ErrorMessage = "Maximum number of files to delete allowed is 50")]
        public List<Guid>? FilesToDelete { get; set; }
    }
} 