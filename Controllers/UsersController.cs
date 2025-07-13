using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAG.Models;
using RAG.Services;

namespace RAG.Controllers
{
    /// <summary>
    /// Controller per la gestione delle configurazioni utente
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
            private readonly SqliteDataService _dataService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(SqliteDataService dataService, ILogger<UsersController> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        /// <summary>
        /// Recupera la configurazione completa dell'utente
        /// </summary>
        /// <param name="userId">ID dell'utente</param>
        /// <returns>Configurazione utente con knowledgeRules e toneRules</returns>
        [HttpGet("{userId}/configuration")]
        public async Task<IActionResult> GetUserConfiguration(Guid userId)
        {
            try
            {
                _logger.LogInformation($"[GetUserConfiguration] Recupero configurazione per userId: {userId}");
                
                // Verifica autorizzazione - l'utente può vedere solo la propria configurazione
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    _logger.LogWarning($"[GetUserConfiguration] Tentativo di accesso non autorizzato. UserId richiesto: {userId}, UserId corrente: {currentUserId}");
                    return Forbid();
                }

                var configuration = await _dataService.GetUserConfigurationAsync(userId);
                
                if (configuration == null)
                {
                    _logger.LogInformation($"[GetUserConfiguration] Configurazione non trovata per userId: {userId}. Creazione configurazione vuota.");
                    
                    // Crea una configurazione vuota se non esiste
                    configuration = new UserConfiguration
                    {
                        UserId = userId,
                        KnowledgeRules = new List<KnowledgeRule>(),
                        ToneRules = new List<ToneRule>(),
                        Files = new List<RAG.Models.File>()
                    };
                    
                    await _dataService.UpdateUserConfigurationAsync(configuration);
                }

                var response = new
                {
                    knowledgeRules = configuration.KnowledgeRules.Select(kr => new
                    {
                        id = kr.Id,
                        content = kr.Content
                    }),
                    toneRules = configuration.ToneRules.Select(tr => new
                    {
                        id = tr.Id,
                        content = tr.Content
                    }),
                    files = configuration.Files.Select(f => new
                    {
                        id = f.Id,
                        name = f.Name,
                        size = f.Size
                    })
                };

                _logger.LogInformation($"[GetUserConfiguration] Configurazione recuperata con successo per userId: {userId}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetUserConfiguration] Errore durante il recupero della configurazione per userId: {userId}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante il recupero della configurazione",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Aggiorna la configurazione completa dell'utente
        /// </summary>
        /// <param name="userId">ID dell'utente</param>
        /// <param name="request">Configurazione da aggiornare</param>
        /// <returns>Risultato dell'operazione</returns>
        [HttpPut("{userId}/configuration")]
        public async Task<IActionResult> UpdateUserConfiguration(Guid userId, [FromBody] UpdateUserConfigurationRequest request)
        {
            try
            {
                _logger.LogInformation($"[UpdateUserConfiguration] Aggiornamento configurazione per userId: {userId}");
                
                // Verifica autorizzazione - l'utente può modificare solo la propria configurazione
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    _logger.LogWarning($"[UpdateUserConfiguration] Tentativo di modifica non autorizzato. UserId richiesto: {userId}, UserId corrente: {currentUserId}");
                    return Forbid();
                }

                if (request == null)
                {
                    return BadRequest(new ErrorResponse { Message = "Richiesta non valida" });
                }

                // Validazione dati
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(new ErrorResponse 
                    { 
                        Message = "Dati non validi", 
                        Details = string.Join(", ", errors) 
                    });
                }

                // Prepara le liste per l'aggiornamento granulare
                var knowledgeRulesToAdd = request.KnowledgeRules?.Select(kr => new KnowledgeRule
                {
                    Id = kr.Id ?? Guid.NewGuid(),
                    Content = kr.Content
                }).ToList();

                var toneRulesToAdd = request.ToneRules?.Select(tr => new RAG.Models.ToneRule
                {
                    Id = tr.Id ?? Guid.NewGuid(),
                    Content = tr.Content
                }).ToList();

                var filesToAdd = request.Files?.Select(f => new RAG.Models.File
                {
                    Id = f.Id ?? Guid.NewGuid(),
                    Name = f.Name,
                    ContentType = f.ContentType,
                    Size = f.Size > 0 ? f.Size : CalculateBase64Size(f.Content),
                    Content = f.Content
                }).ToList();

                var success = await _dataService.UpdateUserConfigurationGranularAsync(userId,
                    knowledgeRulesToAdd,
                    toneRulesToAdd,
                    filesToAdd,
                    request.KnowledgeRulesToDelete,
                    request.ToneRulesToDelete,
                    request.FilesToDelete);
                
                if (success)
                {
                    _logger.LogInformation($"[UpdateUserConfiguration] Configurazione aggiornata con successo per userId: {userId}");
                    return Ok(new SuccessResponse { Message = "Configurazione aggiornata con successo" });
                }
                else
                {
                    _logger.LogError($"[UpdateUserConfiguration] Fallimento nell'aggiornamento della configurazione per userId: {userId}");
                    return StatusCode(500, new ErrorResponse { Message = "Errore durante l'aggiornamento della configurazione" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[UpdateUserConfiguration] Errore durante l'aggiornamento della configurazione per userId: {userId}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante l'aggiornamento della configurazione",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Estrae l'ID dell'utente corrente dal token JWT
        /// </summary>
        /// <returns>ID dell'utente corrente</returns>
        private Guid GetCurrentUserId()
        {
            var userIdString = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Guid.Empty;
            }
            
            return userId;
        }

        /// <summary>
        /// Calcola la dimensione del file dal contenuto Base64
        /// </summary>
        /// <param name="base64Content">Contenuto Base64 del file</param>
        /// <returns>Dimensione in byte</returns>
        private long CalculateBase64Size(string base64Content)
        {
            if (string.IsNullOrEmpty(base64Content))
                return 0;
            
            try
            {
                // Rimuovi eventuali header Base64 (data:application/pdf;base64,)
                var cleanBase64 = base64Content;
                if (base64Content.Contains(","))
                {
                    cleanBase64 = base64Content.Split(',')[1];
                }
                
                // Calcola la dimensione: ogni 4 caratteri Base64 rappresentano 3 byte
                var base64Length = cleanBase64.Length;
                var padding = 0;
                
                // Conta i caratteri di padding
                if (base64Length > 0 && cleanBase64[base64Length - 1] == '=')
                    padding++;
                if (base64Length > 1 && cleanBase64[base64Length - 2] == '=')
                    padding++;
                
                // Formula: (base64Length * 3) / 4 - padding
                return (base64Length * 3) / 4 - padding;
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Request per aggiornare la configurazione utente
    /// </summary>
    public class UpdateUserConfigurationRequest
    {
        public List<KnowledgeRuleRequest>? KnowledgeRules { get; set; }
        public List<ToneRuleRequest>? ToneRules { get; set; }
        public List<FileRequest>? Files { get; set; }
        public List<Guid>? KnowledgeRulesToDelete { get; set; }
        public List<Guid>? ToneRulesToDelete { get; set; }
        public List<Guid>? FilesToDelete { get; set; }
    }

    /// <summary>
    /// Rappresentazione di una knowledge rule nella richiesta
    /// </summary>
    public class KnowledgeRuleRequest
    {
        public Guid? Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Rappresentazione di una tone rule nella richiesta
    /// </summary>
    public class ToneRuleRequest
    {
        public Guid? Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Rappresentazione di un file nella richiesta
    /// </summary>
    public class FileRequest
    {
        public Guid? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Content { get; set; } = string.Empty; // Base64 encoded file content
    }
} 