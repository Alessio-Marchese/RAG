using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAG.Models;
using RAG.Services;
using System.ComponentModel.DataAnnotations;

namespace RAG.Controllers
{
    /// <summary>
    /// Controller per la gestione delle domande non risposte
    /// </summary>
    [ApiController]
    [Route("api/unanswered-questions")]
    public class UnansweredQuestionsController : ControllerBase
    {
        private readonly SqliteDataService _dataService;
        private readonly ILogger<UnansweredQuestionsController> _logger;

        public UnansweredQuestionsController(SqliteDataService dataService, ILogger<UnansweredQuestionsController> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        /// <summary>
        /// Recupera la lista delle domande senza risposta
        /// </summary>
        /// <returns>Lista delle domande non risposte</returns>
        [HttpGet]
        public async Task<IActionResult> GetUnansweredQuestions()
        {
            try
            {
                _logger.LogInformation("[GetUnansweredQuestions] Recupero lista domande non risposte");
                
                var questions = await _dataService.GetUnansweredQuestionsAsync();
                
                var response = questions.Select(q => new
                {
                    id = q.Id,
                    question = q.Question,
                    context = q.Context,
                    timestamp = q.Timestamp,
                    createdAt = q.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                }).ToList();

                _logger.LogInformation($"[GetUnansweredQuestions] Recuperate {questions.Count} domande non risposte");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetUnansweredQuestions] Errore durante il recupero delle domande non risposte");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante il recupero delle domande",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Recupera una domanda specifica
        /// </summary>
        /// <param name="questionId">ID della domanda</param>
        /// <returns>Domanda richiesta</returns>
        [HttpGet("{questionId}")]
        public async Task<IActionResult> GetUnansweredQuestion(Guid questionId)
        {
            try
            {
                _logger.LogInformation($"[GetUnansweredQuestion] Recupero domanda per questionId: {questionId}");
                
                var question = await _dataService.GetUnansweredQuestionAsync(questionId);
                
                if (question == null)
                {
                    _logger.LogWarning($"[GetUnansweredQuestion] Domanda non trovata per questionId: {questionId}");
                    return NotFound(new ErrorResponse { Message = "Domanda non trovata" });
                }

                var response = new
                {
                    id = question.Id,
                    question = question.Question,
                    context = question.Context,
                    timestamp = question.Timestamp,
                    createdAt = question.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                _logger.LogInformation($"[GetUnansweredQuestion] Domanda recuperata con successo per questionId: {questionId}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetUnansweredQuestion] Errore durante il recupero della domanda per questionId: {questionId}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante il recupero della domanda",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Crea una nuova domanda non risposta
        /// </summary>
        /// <param name="request">Dati della domanda</param>
        /// <returns>Domanda creata</returns>
        [HttpPost]
        public async Task<IActionResult> CreateUnansweredQuestion([FromBody] CreateUnansweredQuestionRequest request)
        {
            try
            {
                _logger.LogInformation("[CreateUnansweredQuestion] Creazione nuova domanda non risposta");
                
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

                if (string.IsNullOrWhiteSpace(request.Question))
                {
                    return BadRequest(new ErrorResponse { Message = "La domanda è obbligatoria" });
                }

                var question = await _dataService.CreateUnansweredQuestionAsync(request.Question, request.Context);
                
                var response = new
                {
                    id = question.Id,
                    question = question.Question,
                    context = question.Context,
                    timestamp = question.Timestamp,
                    createdAt = question.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                _logger.LogInformation($"[CreateUnansweredQuestion] Domanda creata con successo, questionId: {question.Id}");
                return CreatedAtAction(nameof(GetUnansweredQuestion), new { questionId = question.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CreateUnansweredQuestion] Errore durante la creazione della domanda");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante la creazione della domanda",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Fornisce una risposta a una domanda (sposta la coppia Q&A nella knowledge base)
        /// </summary>
        /// <param name="questionId">ID della domanda da rispondere</param>
        /// <param name="request">Risposta e ID utente</param>
        /// <returns>Risultato dell'operazione</returns>
        [HttpPost("{questionId}/answer")]
        public async Task<IActionResult> AnswerQuestion(Guid questionId, [FromBody] AnswerQuestionRequest request)
        {
            try
            {
                _logger.LogInformation($"[AnswerQuestion] Risposta alla domanda per questionId: {questionId}");
                
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

                if (string.IsNullOrWhiteSpace(request.Answer))
                {
                    return BadRequest(new ErrorResponse { Message = "La risposta è obbligatoria" });
                }

                var success = await _dataService.AnswerQuestionAsync(questionId, request);
                
                if (success)
                {
                    _logger.LogInformation($"[AnswerQuestion] Domanda risposta con successo per questionId: {questionId}");
                    return Ok(new SuccessResponse { Message = "Domanda risposta con successo. La coppia Q&A è stata aggiunta alla knowledge base." });
                }
                else
                {
                    _logger.LogWarning($"[AnswerQuestion] Domanda non trovata per questionId: {questionId}");
                    return NotFound(new ErrorResponse { Message = "Domanda non trovata" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AnswerQuestion] Errore durante la risposta alla domanda per questionId: {questionId}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante la risposta alla domanda",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Scarta una domanda non pertinente
        /// </summary>
        /// <param name="questionId">ID della domanda da scartare</param>
        /// <returns>Risultato dell'operazione</returns>
        [HttpDelete("{questionId}")]
        public async Task<IActionResult> DeleteUnansweredQuestion(Guid questionId)
        {
            try
            {
                _logger.LogInformation($"[DeleteUnansweredQuestion] Eliminazione domanda per questionId: {questionId}");
                
                var success = await _dataService.DeleteUnansweredQuestionAsync(questionId);
                
                if (success)
                {
                    _logger.LogInformation($"[DeleteUnansweredQuestion] Domanda eliminata con successo per questionId: {questionId}");
                    return Ok(new SuccessResponse { Message = "Domanda eliminata con successo" });
                }
                else
                {
                    _logger.LogWarning($"[DeleteUnansweredQuestion] Domanda non trovata per questionId: {questionId}");
                    return NotFound(new ErrorResponse { Message = "Domanda non trovata" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DeleteUnansweredQuestion] Errore durante l'eliminazione della domanda per questionId: {questionId}");
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Errore interno del server durante l'eliminazione della domanda",
                    Details = ex.Message
                });
            }
        }
    }

    /// <summary>
    /// Request per creare una nuova domanda non risposta
    /// </summary>
    public class CreateUnansweredQuestionRequest
    {
        public string Question { get; set; } = string.Empty;
        public string? Context { get; set; }
    }
} 