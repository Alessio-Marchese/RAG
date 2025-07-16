using System.Text;
using System.Text.Json;

namespace RAG.Services
{
    public interface IPineconeService
    {
        Task<bool> DeleteAllEmbeddingsInNamespaceAsync(string namespaceName);
    }

    public class PineconeService : IPineconeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _indexHost;

        public PineconeService(HttpClient httpClient, string apiKey, string indexHost)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
            _indexHost = indexHost;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Api-Key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("X-Pinecone-API-Version", "2025-04");
        }

        /// <summary>
        /// Elimina tutti gli embedding all'interno di un namespace specifico in un indice Pinecone.
        /// </summary>
        /// <param name="namespaceName">Il nome del namespace da cui eliminare gli embedding.</param>
        /// <returns>True se l'operazione ha avuto successo, altrimenti False.</returns>
        public async Task<bool> DeleteAllEmbeddingsInNamespaceAsync(string namespaceName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                Console.WriteLine("Il nome del namespace non può essere vuoto o nullo.");
                return false;
            }

            // Crea l'oggetto JSON per la richiesta
            var requestBody = new
            {
                deleteAll = true,
                @namespace = namespaceName
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                // Effettua la richiesta POST sull'host specifico dell'indice
                var url = $"https://{_indexHost}/vectors/delete";
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Tutti gli embedding nel namespace '{namespaceName}' sono stati eliminati con successo.");
                    return true;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Errore durante l'eliminazione degli embedding: {response.StatusCode} - {errorContent}");
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Errore di rete o HTTP: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Si è verificato un errore inatteso: {ex.Message}");
                return false;
            }
        }
    }
}