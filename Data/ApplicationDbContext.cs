using Microsoft.EntityFrameworkCore;
using RAG.Entities;
using FileEntity = RAG.Entities.File;

namespace RAG.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}
        public DbSet<KnowledgeRule> KnowledgeRules { get; set; }
        public DbSet<FileEntity> Files { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<KnowledgeRule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.HasIndex(e => e.UserId);
            });

            modelBuilder.Entity<FileEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Size).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.HasIndex(e => e.UserId);
            });
        }
    }
} 