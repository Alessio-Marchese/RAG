using Microsoft.EntityFrameworkCore;
using RAG.Data;
using FileEntity = RAG.Entities.File;

namespace RAG.Repositories
{
    public interface IFileRepository
    {
        Task<List<FileEntity>> GetByUserIdAsync(Guid userId);
        Task<List<FileEntity>> GetByUserIdPaginatedAsync(Guid userId, int skip, int take);
        Task<List<string>> GetFileNamesByUserIdAsync(Guid userId);
        Task<List<string>> GetFileNamesByIdsAsync(Guid userId, List<Guid> fileIds);
        Task<List<FileEntity>> GetByIdsAsync(List<Guid> ids);
        Task<FileEntity> CreateAsync(FileEntity file);
        Task<bool> DeleteMultipleAsync(List<Guid> ids, Guid userId);
    }

    public class FileRepository : IFileRepository
    {
        private readonly ApplicationDbContext _context;

        public FileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FileEntity> CreateAsync(FileEntity file)
        {
            _context.Files.Add(file);
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<List<FileEntity>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Files
                .Where(f => f.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<FileEntity>> GetByUserIdPaginatedAsync(Guid userId, int skip, int take)
        {
            return await _context.Files
                .Where(f => f.UserId == userId)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<string>> GetFileNamesByUserIdAsync(Guid userId)
        {
            return await _context.Files
                .Where(f => f.UserId == userId)
                .Select(f => f.Name)
                .ToListAsync();
        }

        public async Task<List<string>> GetFileNamesByIdsAsync(Guid userId, List<Guid> fileIds)
        {
            return await _context.Files
                .Where(f => fileIds.Contains(f.Id) && f.UserId == userId)
                .Select(f => f.Name)
                .ToListAsync();
        }

        public async Task<List<FileEntity>> GetByIdsAsync(List<Guid> ids)
        {
            return await _context.Files
                .Where(f => ids.Contains(f.Id))
                .ToListAsync();
        }

        public async Task<bool> DeleteMultipleAsync(List<Guid> ids, Guid userId)
        {
            var filesToRemove = await _context.Files
                .Where(f => ids.Contains(f.Id) && f.UserId == userId)
                .ToListAsync();

            if (filesToRemove.Any())
                _context.Files.RemoveRange(filesToRemove);

            return true;
        }
    }
} 