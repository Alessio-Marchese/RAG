using Alessio.Marchese.Utils.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RAG.Entities;
using RAG.Configuration;

namespace RAG.Services
{
    public interface IS3Service
    {
        Task<Result> UploadFileAsync(Guid userId, IFormFile file, string fileName);
        Task<Result> DeleteFileAsync(Guid userId, string fileName);
        Task<Result> UpdateKnowledgeRulesFileAsync(Guid userId, List<KnowledgeRule> knowledgeRules);
    }

    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3Service(IAmazonS3 s3Client, IOptions<AppConfiguration> configuration)
        {
            _s3Client = s3Client;
            _bucketName = configuration.Value.AWS.BucketName;
        }

        public async Task<Result> UploadFileAsync(Guid userId, IFormFile file, string fileName)
        {
            using var stream = file.OpenReadStream();
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = $"{userId}/{fileName}",
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(putRequest);
            return Result.Success();
        }

        public async Task<Result> DeleteFileAsync(Guid userId, string fileName)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = $"{userId}/{fileName}"
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
            return Result.Success();
        }

        public async Task<Result> UpdateKnowledgeRulesFileAsync(Guid userId, List<KnowledgeRule> knowledgeRules)
        {
            var deleteResult = await DeleteFileAsync(userId, "knowledge-rules.txt");
            if (!deleteResult.IsSuccessful)
                return deleteResult.ToResult();

            if (!knowledgeRules.Any())
                return Result.Success();

            var content = string.Join("\n\n", knowledgeRules.Select(kr => kr.Content));
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);

            using var stream = new MemoryStream(contentBytes);
            var formFile = new FormFile(stream, 0, contentBytes.Length, "knowledge-rules.txt", "knowledge-rules.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            var uploadResult = await UploadFileAsync(userId, formFile, "knowledge-rules.txt");
            if (!uploadResult.IsSuccessful)
                return uploadResult.ToResult();

            return Result.Success();
        }
    }
} 