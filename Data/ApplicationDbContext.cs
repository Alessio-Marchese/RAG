using Microsoft.EntityFrameworkCore;
using RAG.Entities;
using FileEntity = RAG.Entities.File;

namespace RAG.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<UserConfiguration> UserConfigurations { get; set; }
        public DbSet<KnowledgeRule> KnowledgeRules { get; set; }
        public DbSet<UnansweredQuestion> UnansweredQuestions { get; set; }
        public DbSet<FileEntity> Files { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserConfiguration>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).IsRequired();
                
            });

            modelBuilder.Entity<KnowledgeRule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Content).IsRequired();
                
                entity.Property<Guid>("UserId").IsRequired();
                entity.HasIndex("UserId");
            });

            modelBuilder.Entity<UnansweredQuestion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Question).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<FileEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Size).IsRequired();
                entity.Property(e => e.Content).IsRequired();
                
                entity.Property<Guid>("UserId").IsRequired();
                entity.HasIndex("UserId");
            });

            modelBuilder.Entity<KnowledgeRule>()
                .HasIndex("UserId")
                .HasDatabaseName("IX_KnowledgeRules_UserId");

            modelBuilder.Entity<FileEntity>()
                .HasIndex("UserId")
                .HasDatabaseName("IX_Files_UserId");
        }
    }
} 