using System.ComponentModel.DataAnnotations;

namespace RAG.Entities
{
    public class File
    {
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string ContentType { get; set; } = string.Empty;
        [Required]
        public long Size { get; set; }
        [Required]
        public string Content { get; set; } = string.Empty; // Base64 encoded file content
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
} 