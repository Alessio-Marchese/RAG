namespace RAG.Entities
{
    public class ErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
} 