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

        public async Task<bool> DeleteAllEmbeddingsInNamespaceAsync(string namespaceName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                return false;

            var requestBody = new
            {
                deleteAll = true,
                @namespace = namespaceName
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                var url = $"https://{_indexHost}/vectors/delete";
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                    return true;
                else    
                    return false;
            }
            catch
            {
                return false;
            }
        }
    }
}