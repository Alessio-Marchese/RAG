using Microsoft.EntityFrameworkCore;
using RAG.Data;
using RAG.Repositories;
using Alessio.Marchese.Utils.Core;

namespace RAG.Services
{
    public interface IUnitOfWork : IDisposable
    {
        IKnowledgeRuleRepository KnowledgeRules { get; }
        IFileRepository Files { get; }
        
        Task<Result> SaveChangesAsync();
        Task<Result> ExecuteTransactionAsync(Func<Task<Result>>[] operations);
        Task<Result> ExecuteTransactionAsync(Func<Task>[] operations);
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IKnowledgeRuleRepository? _knowledgeRuleRepository;
        private IFileRepository? _fileRepository;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IKnowledgeRuleRepository KnowledgeRules
        {
            get
            {
                _knowledgeRuleRepository ??= new KnowledgeRuleRepository(_context);
                return _knowledgeRuleRepository;
            }
        }

        public IFileRepository Files
        {
            get
            {
                _fileRepository ??= new FileRepository(_context);
                return _fileRepository;
            }
        }

        public async Task<Result> SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                return Result.Success();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Result.Failure($"Database concurrency error occurred while saving changes: {ex.Message}. The data may have been modified by another user.");
            }
            catch (DbUpdateException ex)
            {
                return Result.Failure($"Database update error occurred while saving changes: {ex.Message}. Please check your data and try again.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Unexpected database error occurred while saving changes: {ex.Message}");
            }
        }

        public async Task<Result> ExecuteTransactionAsync(Func<Task<Result>>[] operations)
        {
            try
            {
                if (_context.Database.CurrentTransaction != null)
                {
                    foreach (var operation in operations)
                    {
                        var result = await operation();
                        if (!result.IsSuccessful)
                        {
                            return result;
                        }
                    }
                    return Result.Success();
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    foreach (var operation in operations)
                    {
                        var result = await operation();
                        if (!result.IsSuccessful)
                        {
                            await transaction.RollbackAsync();
                            return result;
                        }
                    }

                    await transaction.CommitAsync();
                    return Result.Success();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure($"Transaction failed and was rolled back: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to execute database transaction: {ex.Message}");
            }
        }

        public async Task<Result> ExecuteTransactionAsync(Func<Task>[] operations)
        {
            try
            {
                if (_context.Database.CurrentTransaction != null)
                {
                    foreach (var operation in operations)
                    {
                        await operation();
                    }
                    return Result.Success();
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    foreach (var operation in operations)
                    {
                        await operation();
                    }

                    await transaction.CommitAsync();
                    return Result.Success();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure($"Transaction failed and was rolled back: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to execute database transaction: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
} 