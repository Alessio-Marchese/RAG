using Microsoft.EntityFrameworkCore;
using RAG.Models;

namespace RAG.Data
{
    /// <summary>
    /// Context del database per l'applicazione RAG
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<UserConfiguration> UserConfigurations { get; set; }
        public DbSet<KnowledgeRule> KnowledgeRules { get; set; }
        public DbSet<ToneRule> ToneRules { get; set; }
        public DbSet<UnansweredQuestion> UnansweredQuestions { get; set; }
        public DbSet<RAG.Models.File> Files { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurazione UserConfiguration
            modelBuilder.Entity<UserConfiguration>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                
                // Non definire le relazioni qui per evitare foreign key duplicate
                // Le relazioni sono gestite tramite shadow properties nelle entità figlie
            });

            // Configurazione KnowledgeRule
            modelBuilder.Entity<KnowledgeRule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FileName).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                
                // Shadow property per la foreign key
                entity.Property<Guid>("UserId").IsRequired();
                entity.HasIndex("UserId");
            });

            // Configurazione ToneRule
            modelBuilder.Entity<ToneRule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                
                // Shadow property per la foreign key
                entity.Property<Guid>("UserId").IsRequired();
                entity.HasIndex("UserId");
            });

            // Configurazione UnansweredQuestion
            modelBuilder.Entity<UnansweredQuestion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Question).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Context).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // Configurazione File
            modelBuilder.Entity<RAG.Models.File>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Size).IsRequired();
                entity.Property(e => e.Content).IsRequired(); // Base64 content può essere molto grande
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                
                // Shadow property per la foreign key
                entity.Property<Guid>("UserId").IsRequired();
                entity.HasIndex("UserId");
            });

            // Indici per performance
            modelBuilder.Entity<KnowledgeRule>()
                .HasIndex("UserId", "CreatedAt")
                .HasDatabaseName("IX_KnowledgeRules_UserId_CreatedAt");

            modelBuilder.Entity<ToneRule>()
                .HasIndex("UserId", "CreatedAt")
                .HasDatabaseName("IX_ToneRules_UserId_CreatedAt");

            modelBuilder.Entity<UnansweredQuestion>()
                .HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_UnansweredQuestions_CreatedAt");

            modelBuilder.Entity<RAG.Models.File>()
                .HasIndex("UserId", "CreatedAt")
                .HasDatabaseName("IX_Files_UserId_CreatedAt");
        }
    }
} 