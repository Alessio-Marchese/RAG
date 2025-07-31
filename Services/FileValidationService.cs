using Alessio.Marchese.Utils.Core;

namespace RAG.Services
{
    public interface IFileValidationService
    {
        Task<Result> ValidateFileAsync(string fileName, string contentType, long size);
    }

    public class FileValidationService : IFileValidationService
    {
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = { ".txt", ".pdf", ".doc", ".docx", ".rtf" };
        private static readonly string[] AllowedMimeTypes = { 
            "text/plain", 
            "application/pdf", 
            "application/msword", 
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/rtf" 
        };

        public Task<Result> ValidateFileAsync(string fileName, string contentType, long size)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return Task.FromResult(Result.Failure("File name is required and cannot be empty"));

            if (string.IsNullOrWhiteSpace(contentType))
                return Task.FromResult(Result.Failure("Content type is required and cannot be empty"));

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return Task.FromResult(Result.Failure($"File type '{extension}' is not allowed. Allowed file types: {string.Join(", ", AllowedExtensions)}"));

            if (size > MaxFileSizeBytes)
                return Task.FromResult(Result.Failure($"File size ({size} bytes) exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)}MB"));

            return Task.FromResult(Result.Success());
        }
    }
} 