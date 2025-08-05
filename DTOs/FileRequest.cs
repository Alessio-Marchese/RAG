using System.ComponentModel.DataAnnotations;

namespace RAG.DTOs
{
    public class FileRequest
    {
        [Required(ErrorMessage = "File name is required and cannot be empty")]
        [StringLength(500, ErrorMessage = "File name cannot exceed 500 characters")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Content type is required and cannot be empty")]
        [StringLength(255, ErrorMessage = "Content type cannot exceed 255 characters")]
        public string ContentType { get; set; } = string.Empty;
        [Range(1, long.MaxValue, ErrorMessage = "File size must be greater than 0")]
        public long Size { get; set; }
        
        [Required(ErrorMessage = "File content is required and cannot be empty")]
        public string Content { get; set; } = string.Empty;
    }
} 