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
        public string Content { get; set; } = string.Empty;
    }
} 