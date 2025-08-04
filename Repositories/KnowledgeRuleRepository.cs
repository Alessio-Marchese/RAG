using Microsoft.EntityFrameworkCore;
using RAG.Data;
using RAG.Entities;

namespace RAG.Repositories
{
    public interface IKnowledgeRuleRepository
    {
        Task<List<KnowledgeRule>> GetByUserIdAsync(Guid userId);
        Task<List<KnowledgeRule>> GetByUserIdPaginatedAsync(Guid userId, int skip, int take);
        Task<List<KnowledgeRule>> GetByIdsAsync(List<Guid> ids);
        Task<KnowledgeRule> CreateAsync(KnowledgeRule knowledgeRule);
        Task<bool> DeleteMultipleAsync(List<Guid> ids, Guid userId);
    }

    public class KnowledgeRuleRepository : IKnowledgeRuleRepository
    {
        private readonly ApplicationDbContext _context;

        public KnowledgeRuleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<KnowledgeRule> CreateAsync(KnowledgeRule knowledgeRule)
        {
            _context.KnowledgeRules.Add(knowledgeRule);
            return knowledgeRule;
        }

        public async Task<List<KnowledgeRule>> GetByUserIdAsync(Guid userId)
        {
            return await _context.KnowledgeRules
                .Where(kr => kr.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<KnowledgeRule>> GetByUserIdPaginatedAsync(Guid userId, int skip, int take)
        {
            return await _context.KnowledgeRules
                .Where(kr => kr.UserId == userId)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<KnowledgeRule>> GetByIdsAsync(List<Guid> ids)
        {
            return await _context.KnowledgeRules
                .Where(kr => ids.Contains(kr.Id))
                .ToListAsync();
        }

        public async Task<bool> DeleteMultipleAsync(List<Guid> ids, Guid userId)
        {
            var rulesToRemove = await _context.KnowledgeRules
                .Where(kr => ids.Contains(kr.Id) && kr.UserId == userId)
                .ToListAsync();

            if (rulesToRemove.Any())
                _context.KnowledgeRules.RemoveRange(rulesToRemove);

            return true;
        }
    }
} 