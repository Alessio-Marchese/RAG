namespace RAG.DTOs
{
    public class UserConfigurationResponse
    {
        public List<KnowledgeRuleResponse> KnowledgeRules { get; set; } = new List<KnowledgeRuleResponse>();
        public List<FileResponse> Files { get; set; } = new List<FileResponse>();
    }

    public class KnowledgeRuleResponse
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class FileResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
    }
} 