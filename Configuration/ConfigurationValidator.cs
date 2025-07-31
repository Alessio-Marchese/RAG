using Microsoft.Extensions.Options;

namespace RAG.Configuration
{
    public class ConfigurationValidator : IValidateOptions<AppConfiguration>
    {
        public ValidateOptionsResult Validate(string? name, AppConfiguration options)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(options.AWS.AccessKey))
                errors.Add("AWS AccessKey is required for S3 storage operations");
            if (string.IsNullOrWhiteSpace(options.AWS.SecretKey))
                errors.Add("AWS SecretKey is required for S3 storage operations");
            if (string.IsNullOrWhiteSpace(options.AWS.Region))
                errors.Add("AWS Region is required for S3 storage operations");
            if (string.IsNullOrWhiteSpace(options.AWS.BucketName))
                errors.Add("AWS BucketName is required for S3 storage operations");

            if (string.IsNullOrWhiteSpace(options.Jwt.Key))
                errors.Add("JWT Key is required for token signing and validation");
            else if (options.Jwt.Key.Length < 32)
                errors.Add("JWT Key must be at least 32 characters long to ensure security");
            
            if (string.IsNullOrWhiteSpace(options.Jwt.Issuer))
                errors.Add("JWT Issuer is required for token validation");
            if (string.IsNullOrWhiteSpace(options.Jwt.Audience))
                errors.Add("JWT Audience is required for token validation");
            if (options.Jwt.ExpirationMinutes <= 0)
                errors.Add("JWT ExpirationMinutes must be greater than 0");
            if (options.Jwt.ExpirationMinutes > 1440) // 24 ore
                errors.Add("JWT ExpirationMinutes cannot exceed 1440 minutes (24 hours) for security reasons");

            if (string.IsNullOrWhiteSpace(options.Pinecone.ApiKey))
                errors.Add("Pinecone ApiKey is required for vector database operations");
            if (string.IsNullOrWhiteSpace(options.Pinecone.IndexHost))
                errors.Add("Pinecone IndexHost is required for vector database operations");

            if (string.IsNullOrWhiteSpace(options.ConnectionStrings.DefaultConnection))
                errors.Add("Database ConnectionString is required for data persistence");

            return errors.Count > 0 
                ? ValidateOptionsResult.Fail(errors) 
                : ValidateOptionsResult.Success;
        }
    }
} 