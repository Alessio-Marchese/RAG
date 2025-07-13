using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAG.Models;
using RAG.Services;

namespace RAG.Controllers
{
    /// <summary>
    /// Controller per la gestione delle knowledge rules
    /// </summary>
    [ApiController]
    [Route("api/users/{userId}/knowledge-rules")]
    [Authorize]
    public class KnowledgeRulesController : ControllerBase
    {
        private readonly SqliteDataService _dataService;
        private readonly IUserConfigService _userConfigService;
        private readonly ILogger<KnowledgeRulesController> _logger;

        public KnowledgeRulesController(
            SqliteDataService dataService, 
            IUserConfigService userConfigService,
            ILogger<KnowledgeRulesController> logger)
        {
            _dataService = dataService;
            _userConfigService = userConfigService;
            _logger = logger;
        }

        /// <summary>
        /// Aggiunge un nuovo item testuale alla knowledge base
        /// </summary>
        /// <param name="userId">ID dell'utente</param>
        /// <param name="request">Contenuto e tipo della knowledge rule</param>
        /// <returns>Knowledge rule creata</returns>
        [HttpPost]
        public async Task<IActionResult> CreateKnowledgeRule(Guid userId, [FromBody] CreateKnowledgeRuleRequest request)
        {
            try
            {
                _logger.LogInformation($"[CreateKnowledgeRule] Creazione knowledge rule per userId: {userId}");
                
                // Verifica autorizzazione
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    _logger.LogWarning($"[CreateKnowledgeRule] Tentativo di accesso non autorizzato. UserId richiesto: {userId}, UserId corrente: {currentUserId}");
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

                var knowledgeRule = await _dataService.CreateKnowledgeRuleAsync(userId, request);
                
                var response = new
                {
                    id = knowledgeRule.Id,
                    content = knowledgeRule.Content,
                    createdAt = knowledgeRule.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                _logger.LogInformation($"[CreateKnowledgeRule] Knowledge rule creata con successo per userId: {userId}, ruleId: {knowledgeRule.Id}");
                return CreatedAtAction(nameof(GetKnowledgeRule), new { userId, ruleId = knowledgeRule.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[CreateKnowledgeRule] Errore durante la creazione della knowledge rule per userId: {userId}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante la creazione della knowledge rule",
                    Details = ex.Message
                });
            }
        }



        /// <summary>
        /// Recupera una knowledge rule specifica
        /// </summary>
        /// <param name="userId">ID dell'utente</param>
        /// <param name="ruleId">ID della knowledge rule</param>
        /// <returns>Knowledge rule richiesta</returns>
        [HttpGet("{ruleId}")]
        public async Task<IActionResult> GetKnowledgeRule(Guid userId, Guid ruleId)
        {
            try
            {
                _logger.LogInformation($"[GetKnowledgeRule] Recupero knowledge rule per userId: {userId}, ruleId: {ruleId}");
                
                // Verifica autorizzazione
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    _logger.LogWarning($"[GetKnowledgeRule] Tentativo di accesso non autorizzato. UserId richiesto: {userId}, UserId corrente: {currentUserId}");
                    return Forbid();
                }

                var knowledgeRule = await _dataService.GetKnowledgeRuleAsync(userId, ruleId);
                
                if (knowledgeRule == null)
                {
                    _logger.LogWarning($"[GetKnowledgeRule] Knowledge rule non trovata per userId: {userId}, ruleId: {ruleId}");
                    return NotFound(new ErrorResponse { Message = "Knowledge rule non trovata" });
                }

                var response = new
                {
                    id = knowledgeRule.Id,
                    content = knowledgeRule.Content,
                    createdAt = knowledgeRule.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    updatedAt = knowledgeRule.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                _logger.LogInformation($"[GetKnowledgeRule] Knowledge rule recuperata con successo per userId: {userId}, ruleId: {ruleId}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetKnowledgeRule] Errore durante il recupero della knowledge rule per userId: {userId}, ruleId: {ruleId}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante il recupero della knowledge rule",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Modifica una knowledge rule esistente
        /// </summary>
        /// <param name="userId">ID dell'utente</param>
        /// <param name="ruleId">ID della knowledge rule da modificare</param>
        /// <param name="request">Nuovi dati della knowledge rule</param>
        /// <returns>Risultato dell'operazione</returns>
        [HttpPut("{ruleId}")]
        public async Task<IActionResult> UpdateKnowledgeRule(Guid userId, Guid ruleId, [FromBody] UpdateKnowledgeRuleRequest request)
        {
            try
            {
                _logger.LogInformation($"[UpdateKnowledgeRule] Aggiornamento knowledge rule per userId: {userId}, ruleId: {ruleId}");
                
                // Verifica autorizzazione
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    _logger.LogWarning($"[UpdateKnowledgeRule] Tentativo di accesso non autorizzato. UserId richiesto: {userId}, UserId corrente: {currentUserId}");
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

                var success = await _dataService.UpdateKnowledgeRuleAsync(userId, ruleId, request);
                
                if (success)
                {
                    _logger.LogInformation($"[UpdateKnowledgeRule] Knowledge rule aggiornata con successo per userId: {userId}, ruleId: {ruleId}");
                    return Ok(new SuccessResponse { Message = "Knowledge rule aggiornata con successo" });
                }
                else
                {
                    _logger.LogWarning($"[UpdateKnowledgeRule] Knowledge rule non trovata per userId: {userId}, ruleId: {ruleId}");
                    return NotFound(new ErrorResponse { Message = "Knowledge rule non trovata" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[UpdateKnowledgeRule] Errore durante l'aggiornamento della knowledge rule per userId: {userId}, ruleId: {ruleId}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante l'aggiornamento della knowledge rule",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Elimina una knowledge rule
        /// </summary>
        /// <param name="userId">ID dell'utente</param>
        /// <param name="ruleId">ID della knowledge rule da eliminare</param>
        /// <returns>Risultato dell'operazione</returns>
        [HttpDelete("{ruleId}")]
        public async Task<IActionResult> DeleteKnowledgeRule(Guid userId, Guid ruleId)
        {
            try
            {
                _logger.LogInformation($"[DeleteKnowledgeRule] Eliminazione knowledge rule per userId: {userId}, ruleId: {ruleId}");
                
                // Verifica autorizzazione
                var currentUserId = GetCurrentUserId();
                if (currentUserId != userId)
                {
                    _logger.LogWarning($"[DeleteKnowledgeRule] Tentativo di accesso non autorizzato. UserId richiesto: {userId}, UserId corrente: {currentUserId}");
                    return Forbid();
                }

                var success = await _dataService.DeleteKnowledgeRuleAsync(userId, ruleId);
                
                if (success)
                {
                    _logger.LogInformation($"[DeleteKnowledgeRule] Knowledge rule eliminata con successo per userId: {userId}, ruleId: {ruleId}");
                    return Ok(new SuccessResponse { Message = "Knowledge rule eliminata con successo" });
                }
                else
                {
                    _logger.LogWarning($"[DeleteKnowledgeRule] Knowledge rule non trovata per userId: {userId}, ruleId: {ruleId}");
                    return NotFound(new ErrorResponse { Message = "Knowledge rule non trovata" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DeleteKnowledgeRule] Errore durante l'eliminazione della knowledge rule per userId: {userId}, ruleId: {ruleId}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante l'eliminazione della knowledge rule",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Estrae il testo da un PDF utilizzando PdfPig
        /// </summary>
        /// <param name="pdfStream">Stream del PDF</param>
        /// <returns>Testo estratto</returns>


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