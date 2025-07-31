using RAG.DTOs;
using RAG.Entities;

namespace RAG.Mappers
{
    public static class DtoToEntity
    {
        public static Entities.File ToEntity(this FileRequest dto)
            => new()
            {
                Name = dto.Name,
                ContentType = dto.ContentType,
                Size = dto.Size
            };

        public static KnowledgeRule ToEntity(this KnowledgeRuleRequest dto)
            => new()
            {
                Content = dto.Content
            };

        public static KnowledgeRule ToEntity(this KnowledgeRuleResponse dto)
            => new()
            {
                Id = dto.Id,
                Content = dto.Content
            };

    }
} 