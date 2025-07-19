namespace RAG.DTOs
{
    public class FileRequest
    {
        public Guid? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Content { get; set; } = string.Empty;
    }
} 