using Amazon.S3;
using Amazon.S3.Transfer;

namespace RAG.Services
{
    public interface IS3StorageService
    {
        Task UploadFileAsync(string userId, IFormFile file, string? fileName = null);
        Task<bool> DeleteAllUserFilesAsync(string userId);
    }   

    public class S3StorageService : IS3StorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3StorageService(IAmazonS3 s3Client, IConfiguration config)
        {
            _s3Client = s3Client;
            _bucketName = config["AWS:BucketName"] ?? throw new InvalidOperationException("AWS:BucketName is not configured");
        }

        public async Task UploadFileAsync(string userId, IFormFile file, string? fileName = null)
        {
            try
            {
            var key = $"{userId}/{Guid.NewGuid()}_{fileName ?? file.FileName}";
            using var stream = file.OpenReadStream();
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = key,
                BucketName = _bucketName,
                ContentType = file.ContentType
            };
            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(uploadRequest);
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "Nessun dettaglio aggiuntivo";
                throw new Exception($"Errore durante l'upload del file {fileName ?? file.FileName} su S3 per l'utente {userId}: {ex.Message}. Dettagli: {innerMessage}", ex);
            }
        }

        public async Task<bool> DeleteAllUserFilesAsync(string userId)
        {
            try
            {
                var listRequest = new Amazon.S3.Model.ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = $"{userId}/"
                };
                var listResponse = await _s3Client.ListObjectsV2Async(listRequest);
                if (listResponse?.S3Objects != null && listResponse.S3Objects.Count > 0)
                {
                    var deleteRequest = new Amazon.S3.Model.DeleteObjectsRequest
                    {
                        BucketName = _bucketName,
                        Objects = listResponse.S3Objects.Select(o => new Amazon.S3.Model.KeyVersion { Key = o.Key }).ToList()
                    };
                    await _s3Client.DeleteObjectsAsync(deleteRequest);
                }
                return true;
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "Nessun dettaglio aggiuntivo";
                throw new Exception($"Errore durante l'eliminazione dei file dall'S3 per l'utente {userId}: {ex.Message}. Dettagli: {innerMessage}", ex);
            }
        }
    }
} 