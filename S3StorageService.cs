using Amazon.S3;
using Amazon.S3.Transfer;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public interface IS3StorageService
{
    Task<string> UploadFileAsync(string userId, IFormFile file, string? fileName = null);
    Task DeleteAllUserFilesAsync(string userId);
}

public class S3StorageService : IS3StorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IAmazonS3 s3Client, IConfiguration config, ILogger<S3StorageService> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
        _bucketName = config["AWS:BucketName"];
        if (string.IsNullOrEmpty(_bucketName))
        {
            _logger.LogError("BucketName non configurato correttamente in appsettings.json (AWS:BucketName)");
            throw new Exception("BucketName non configurato correttamente in appsettings.json (AWS:BucketName)");
        }
        _logger.LogInformation($"[S3StorageService] BucketName configurato: {_bucketName}");
    }

    public async Task<string> UploadFileAsync(string userId, IFormFile file, string? fileName = null)
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
        return key;
    }

    public async Task DeleteAllUserFilesAsync(string userId)
    {
        try
        {
            var listRequest = new Amazon.S3.Model.ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = $"{userId}/"
            };
            var listResponse = await _s3Client.ListObjectsV2Async(listRequest);
            if (listResponse == null)
            {
                _logger.LogError($"[DeleteAllUserFilesAsync] listResponse è null per userId={userId}");
                return;
            }
            if (listResponse.S3Objects == null)
            {
                _logger.LogError($"[DeleteAllUserFilesAsync] listResponse.S3Objects è null per userId={userId}");
                return;
            }
            if (listResponse.S3Objects.Count > 0)
            {
                var deleteRequest = new Amazon.S3.Model.DeleteObjectsRequest
                {
                    BucketName = _bucketName,
                    Objects = listResponse.S3Objects.Select(o => new Amazon.S3.Model.KeyVersion { Key = o.Key }).ToList()
                };
                await _s3Client.DeleteObjectsAsync(deleteRequest);
                _logger.LogInformation($"[DeleteAllUserFilesAsync] Eliminati {listResponse.S3Objects.Count} file per userId={userId}");
            }
            else
            {
                _logger.LogInformation($"[DeleteAllUserFilesAsync] Nessun file da eliminare per userId={userId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[DeleteAllUserFilesAsync] Errore durante la cancellazione dei file per userId={userId}");
            throw;
        }
    }
} 