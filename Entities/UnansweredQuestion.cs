using System.ComponentModel.DataAnnotations;

namespace RAG.Entities
{
    public class UnansweredQuestion
    {
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Question { get; set; } = string.Empty;
        [Required]
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }
} 