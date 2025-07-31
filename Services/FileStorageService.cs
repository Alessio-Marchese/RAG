using Microsoft.AspNetCore.Http;
using RAG.DTOs;
using RAG.Entities;
using RAG.Services;
using Alessio.Marchese.Utils.Core;

namespace RAG.Services
{
    public interface IFileStorageService
    {
        Task<Result> UploadFilesAsync(Guid userId, List<FileRequest> files);
        Task<Result> DeleteFilesAsync(Guid userId, List<Guid> fileIds);
        Task<Result> UpdateKnowledgeRulesFileAsync(Guid userId, List<KnowledgeRule> knowledgeRules);
    }

    public class FileStorageService : IFileStorageService
    {
        private readonly IS3Service _s3Service;
        private readonly IPineconeService _pineconeService;
        private readonly IUnitOfWork _unitOfWork;

        public FileStorageService(
            IS3Service s3Service, 
            IPineconeService pineconeService,
            IUnitOfWork unitOfWork)
        {
            _s3Service = s3Service;
            _pineconeService = pineconeService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> UploadFilesAsync(Guid userId, List<FileRequest> files)
        {
            if (files == null || !files.Any())
                return Result.Success();

            var uploadTasks = files.Select(file => UploadSingleFileAsync(userId, file));
            var results = await Task.WhenAll(uploadTasks);

            var failedResults = results.Where(r => !r.IsSuccessful).ToList();
            if (failedResults.Any())
                return Result.Failure($"Failed to upload some files to cloud storage: {string.Join(", ", failedResults.Select(r => r.ErrorMessage))}");

            return Result.Success();
        }

        public async Task<Result> DeleteFilesAsync(Guid userId, List<Guid> fileIds)
        {
            if (fileIds == null || !fileIds.Any())
                return Result.Success();

            var fileNames = await _unitOfWork.Files.GetFileNamesByIdsAsync(userId, fileIds);
            
            var s3DeleteTasks = fileNames.Select(fileName => _s3Service.DeleteFileAsync(userId, fileName));
            var s3Results = await Task.WhenAll(s3DeleteTasks);
            
            var failedS3Results = s3Results.Where(r => !r.IsSuccessful).ToList();
            if (failedS3Results.Any())
                return Result.Failure($"Failed to delete files from S3 storage: {string.Join(", ", failedS3Results.Select(r => r.ErrorMessage))}");

            var pineconeDeleteTasks = fileNames.Select(fileName => 
                _pineconeService.DeleteEmbeddingsByFileNameAsync(userId.ToString(), fileName));
            var pineconeResults = await Task.WhenAll(pineconeDeleteTasks);
            
            var failedPineconeResults = pineconeResults.Where(r => !r.IsSuccessful).ToList();
            if (failedPineconeResults.Any())
                return Result.Failure($"Failed to delete embeddings from Pinecone: {string.Join(", ", failedPineconeResults.Select(r => r.ErrorMessage))}");

            return Result.Success();
        }

        public async Task<Result> UpdateKnowledgeRulesFileAsync(Guid userId, List<KnowledgeRule> knowledgeRules)
        {
            var existingKnowledgeRules = await _unitOfWork.KnowledgeRules.GetByUserIdAsync(userId);
            var hasExistingRules = existingKnowledgeRules.Any();

            var result = await _s3Service.UpdateKnowledgeRulesFileAsync(userId, knowledgeRules);
            if (!result.IsSuccessful)
                return result;

            if (hasExistingRules)
            {
                var deleteResult = await _pineconeService.DeleteEmbeddingsByFileNameAsync(
                    userId.ToString(), "knowledge-rules.txt");
                
                if (!deleteResult.IsSuccessful)
                    return deleteResult;
            }

            return Result.Success();
        }
#region PRIVATE METHODS
        private async Task<Result> UploadSingleFileAsync(Guid userId, FileRequest file)
        {
            if (string.IsNullOrEmpty(file.Content))
                return Result.Failure($"File content is missing for file '{file.Name}'. Please provide valid file content.");

            var bytes = Convert.FromBase64String(file.Content);
            using var stream = new MemoryStream(bytes);
            var formFile = new FormFile(stream, 0, bytes.Length, "file", file.Name)
            {
                Headers = new HeaderDictionary(),
                ContentType = file.ContentType
            };

            return await _s3Service.UploadFileAsync(userId, formFile, file.Name);
        }
#endregion
    }
} 