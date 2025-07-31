using System.Text;
using System.Text.Json;
using Alessio.Marchese.Utils.Core;

namespace RAG.Services
{
    public interface IPineconeService
    {
        Task<Result> DeleteEmbeddingsByFileNameAsync(string namespaceName, string fileName);
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

        public async Task<Result> DeleteEmbeddingsByFileNameAsync(string namespaceName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                return Result.Failure("Namespace name cannot be null or empty");

            if (string.IsNullOrWhiteSpace(fileName))
                return Result.Failure("File name cannot be null or empty");

            var requestBody = new
            {
                filter = new
                {
                    file_name = $"{namespaceName}/{fileName}"
                },
                @namespace = namespaceName
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = $"https://{_indexHost}/vectors/delete";
            HttpResponseMessage response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
                return Result.Success();
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Result.Success();
            }
            else
                return Result.Failure($"Pinecone API returned error: {response.StatusCode} - {response.ReasonPhrase}");
        }


    }
}