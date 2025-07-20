using Amazon.S3;
using Amazon.S3.Model;

namespace RAG.Services
{
    public interface IS3StorageService
    {
        Task DeleteAllUserFilesAsync(string userId);
        Task UploadFileAsync(string userId, IFormFile file, string fileName);
    }
    public class S3StorageService : IS3StorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3StorageService(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _bucketName = configuration["AWS:BucketName"] ?? throw new InvalidOperationException("AWS BucketName is not configured");
        }

        public async Task DeleteAllUserFilesAsync(string userId)
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = $"{userId}/"
            };

            var listResponse = await _s3Client.ListObjectsV2Async(listRequest);
            
            if (listResponse.S3Objects.Any())
            {
                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = _bucketName,
                    Objects = listResponse.S3Objects.Select(obj => new KeyVersion { Key = obj.Key }).ToList()
                };

                await _s3Client.DeleteObjectsAsync(deleteRequest);
            }
        }

        public async Task UploadFileAsync(string userId, IFormFile file, string fileName)
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
        }
    }
} 