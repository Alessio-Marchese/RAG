using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAG.Services;

// Classi legacy per compatibilità con UserConfigService
public class ToneRuleLegacy { public string Content { get; set; } = string.Empty; }
public class KnowledgeRuleDto {
    public string Type { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? FileName { get; set; }
    public string? S3Key { get; set; }
}
public class UserConfigLegacy {
    public string UserId { get; set; } = string.Empty;
    public List<ToneRuleLegacy>? ToneRules { get; set; }
    public List<KnowledgeRuleDto>? KnowledgeRules { get; set; }
}

/// <summary>
/// Controller per la gestione dei file di configurazione utente.
/// Orchestration tra servizi di storage, Pinecone e parsing configurazione.
/// </summary>
[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IS3StorageService _storageService;
    private readonly ILogger<FilesController> _logger;
    private readonly IPineconeService _pineconeService;
    private readonly IUserConfigService _userConfigService;

    public FilesController(IS3StorageService storageService, ILogger<FilesController> logger, IPineconeService pineconeService, IUserConfigService userConfigService)
    {
        _storageService = storageService;
        _logger = logger;
        _pineconeService = pineconeService;
        _userConfigService = userConfigService;
    }

    /// <summary>
    /// Endpoint per l'upload della configurazione utente.
    /// - Valida il token JWT
    /// - Elimina embeddings Pinecone e file S3 precedenti
    /// - Carica la nuova configurazione su S3
    /// </summary>
    [HttpPost("upload")]
    [Authorize]
    public async Task<IActionResult> UploadConfig()
    {
        // Estrazione userId dal token
        var userId = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        // Elimina embeddings Pinecone prima di rimuovere i file S3
        var pineconeNamespace = userId;
        await _pineconeService.DeleteAllEmbeddingsInNamespaceAsync(pineconeNamespace);

        // Parsing della form tramite UserConfigService
        var form = await Request.ReadFormAsync();
        var userConfig = await _userConfigService.ParseUserConfigAsync(form);
        userConfig.UserId = userId;
        var allInfo = _userConfigService.SerializeUserConfig(userConfig);
        var fileNameToUpload = "user_config.txt";
        var fileBytes = System.Text.Encoding.UTF8.GetBytes(allInfo);
        using var stream = new MemoryStream(fileBytes);

        // Rimuove tutti i file precedenti dell'utente su S3
        await _storageService.DeleteAllUserFilesAsync(userId);

        // Carica il nuovo file su S3
        await _storageService.UploadFileAsync(userId, new FormFile(stream, 0, fileBytes.Length, "file", fileNameToUpload)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        }, fileNameToUpload);

        return Ok(new { success = true });
    }
} 