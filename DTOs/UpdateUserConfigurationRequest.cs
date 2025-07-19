using System;
using System.Collections.Generic;

namespace RAG.DTOs
{
    public class UpdateUserConfigurationRequest
    {
        public List<KnowledgeRuleRequest>? KnowledgeRules { get; set; }
        public List<FileRequest>? Files { get; set; }
        public List<Guid>? KnowledgeRulesToDelete { get; set; }
        public List<Guid>? FilesToDelete { get; set; }
    }
} 