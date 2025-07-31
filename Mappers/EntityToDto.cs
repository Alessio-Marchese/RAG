using RAG.DTOs;
using RAG.Entities;

namespace RAG.Mappers
{
    public static class EntityToDto
    {
        public static FileResponse ToDto(this Entities.File entity)
            => new()
            {
                Id = entity.Id,
                Name = entity.Name,
                ContentType = entity.ContentType,
                Size = entity.Size
            };

        public static KnowledgeRuleResponse ToDto(this KnowledgeRule entity)
            => new()
            {
                Id = entity.Id,
                Content = entity.Content
            };

        public static List<FileResponse> ToDtoList(this List<Entities.File> entities)
            => entities.Select(e => e.ToDto()).ToList();

        public static List<KnowledgeRuleResponse> ToDtoList(this List<KnowledgeRule> entities)
            => entities.Select(e => e.ToDto()).ToList();

        public static UserConfigurationResponse ToUserConfigurationResponse(
            this List<KnowledgeRule> knowledgeRules, 
            List<Entities.File> files)
            => new()
            {
                KnowledgeRules = knowledgeRules.ToDtoList(),
                Files = files.ToDtoList()
            };

    }
} 