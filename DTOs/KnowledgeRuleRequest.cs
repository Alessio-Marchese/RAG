namespace RAG.DTOs
{
    public class KnowledgeRuleRequest
    {
        public Guid? Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
} 