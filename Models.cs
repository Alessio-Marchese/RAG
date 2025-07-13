using System.ComponentModel.DataAnnotations;

namespace RAG.Models
{
    /// <summary>
    /// Rappresenta una regola di conoscenza nella knowledge base dell'utente
    /// </summary>
    public class KnowledgeRule
    {
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Rappresenta una regola di tono per definire il comportamento dell'AI
    /// </summary>
    public class ToneRule
    {
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Rappresenta una domanda non risolta nel sistema
    /// </summary>
    public class UnansweredQuestion
    {
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string Question { get; set; } = string.Empty;
        
        [Required]
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        
        public string? Context { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Rappresenta un file salvato nel sistema
    /// </summary>
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

    /// <summary>
    /// Configurazione completa dell'utente
    /// </summary>
    public class UserConfiguration
    {
        [Required]
        public Guid UserId { get; set; } = Guid.NewGuid();
        
        public List<KnowledgeRule> KnowledgeRules { get; set; } = new();
        
        public List<ToneRule> ToneRules { get; set; } = new();
        
        public List<RAG.Models.File> Files { get; set; } = new();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Request per aggiungere una nuova knowledge rule
    /// </summary>
    public class CreateKnowledgeRuleRequest
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request per aggiornare una knowledge rule esistente
    /// </summary>
    public class UpdateKnowledgeRuleRequest
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request per aggiungere una nuova tone rule
    /// </summary>
    public class CreateToneRuleRequest
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request per rispondere a una domanda non risolta
    /// </summary>
    public class AnswerQuestionRequest
    {
        [Required]
        public string Answer { get; set; } = string.Empty;
        
        [Required]
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// Response per le operazioni di successo
    /// </summary>
    public class SuccessResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = "Operation completed successfully";
    }

    /// <summary>
    /// Response per gli errori
    /// </summary>
    public class ErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
    }

    /// <summary>
    /// Response paginata per le liste
    /// </summary>
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasNext => Page < TotalPages;
        public bool HasPrevious => Page > 1;
    }
} 