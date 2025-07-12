using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAG.Models;
using RAG.Services;

namespace RAG.Controllers
{
    /// <summary>
    /// Controller per la gestione delle tone rules
    /// </summary>
    [ApiController]
    [Route("api/users/{userId}/tone-rules")]
    [Authorize]
    public class ToneRulesController : ControllerBase
    {
        private readonly SqliteDataService _dataService;
        private readonly ILogger<ToneRulesController> _logger;

        public ToneRulesController(SqliteDataService dataService, ILogger<ToneRulesController> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        /// <summary>
        /// Aggiunge un nuovo item di comportamento alla configurazione
        /// </summary>
        /// <param name="userId">ID dell'utente</param>
        /// <param name="request">Contenuto della tone rule</param>
        /// <returns>Tone rule creata</returns>
        [HttpPost]
        public async Task<IActionResult> CreateToneRule(Guid userId, [FromBody] CreateToneRuleRequest request)
        {
            try
            {
                _logger.LogInformation($"[CreateToneRule] Creazione tone rule per userId: {userId}");
                
                // Verifica autorizzazione
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    _logger.LogWarning($"[CreateToneRule] Tentativo di accesso non autorizzato. UserId richiesto: {userId}, UserId corrente: {currentUserId}");
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

                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest(new ErrorResponse { Message = "Il contenuto è obbligatorio" });
                }

                var toneRule = await _dataService.CreateToneRuleAsync(userId, request);
                
                var response = new
                {
                    id = toneRule.Id,
                    content = toneRule.Content,
                    createdAt = toneRule.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                _logger.LogInformation($"[CreateToneRule] Tone rule creata con successo per userId: {userId}, ruleId: {toneRule.Id}");
                return CreatedAtAction(nameof(GetToneRule), new { userId, ruleId = toneRule.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[CreateToneRule] Errore durante la creazione della tone rule per userId: {userId}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante la creazione della tone rule",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Recupera una tone rule specifica
        /// </summary>
        /// <param name="userId">ID dell'utente</param>
        /// <param name="ruleId">ID della tone rule</param>
        /// <returns>Tone rule richiesta</returns>
        [HttpGet("{ruleId}")]
        public async Task<IActionResult> GetToneRule(Guid userId, Guid ruleId)
        {
            try
            {
                _logger.LogInformation($"[GetToneRule] Recupero tone rule per userId: {userId}, ruleId: {ruleId}");
                
                // Verifica autorizzazione
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    _logger.LogWarning($"[GetToneRule] Tentativo di accesso non autorizzato. UserId richiesto: {userId}, UserId corrente: {currentUserId}");
                    return Forbid();
                }

                var toneRule = await _dataService.GetToneRuleAsync(userId, ruleId);
                
                if (toneRule == null)
                {
                    _logger.LogWarning($"[GetToneRule] Tone rule non trovata per userId: {userId}, ruleId: {ruleId}");
                    return NotFound(new ErrorResponse { Message = "Tone rule non trovata" });
                }

                var response = new
                {
                    id = toneRule.Id,
                    content = toneRule.Content,
                    createdAt = toneRule.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                _logger.LogInformation($"[GetToneRule] Tone rule recuperata con successo per userId: {userId}, ruleId: {ruleId}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetToneRule] Errore durante il recupero della tone rule per userId: {userId}, ruleId: {ruleId}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante il recupero della tone rule",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Elimina una tone rule
        /// </summary>
        /// <param name="userId">ID dell'utente</param>
        /// <param name="ruleId">ID della tone rule da eliminare</param>
        /// <returns>Risultato dell'operazione</returns>
        [HttpDelete("{ruleId}")]
        public async Task<IActionResult> DeleteToneRule(Guid userId, Guid ruleId)
        {
            try
            {
                _logger.LogInformation($"[DeleteToneRule] Eliminazione tone rule per userId: {userId}, ruleId: {ruleId}");
                
                // Verifica autorizzazione
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    _logger.LogWarning($"[DeleteToneRule] Tentativo di accesso non autorizzato. UserId richiesto: {userId}, UserId corrente: {currentUserId}");
                    return Forbid();
                }

                var success = await _dataService.DeleteToneRuleAsync(userId, ruleId);
                
                if (success)
                {
                    _logger.LogInformation($"[DeleteToneRule] Tone rule eliminata con successo per userId: {userId}, ruleId: {ruleId}");
                    return Ok(new SuccessResponse { Message = "Tone rule eliminata con successo" });
                }
                else
                {
                    _logger.LogWarning($"[DeleteToneRule] Tone rule non trovata per userId: {userId}, ruleId: {ruleId}");
                    return NotFound(new ErrorResponse { Message = "Tone rule non trovata" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DeleteToneRule] Errore durante l'eliminazione della tone rule per userId: {userId}, ruleId: {ruleId}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante l'eliminazione della tone rule",
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
    }
} 