using Microsoft.EntityFrameworkCore;
using SoundEffectLibrary.Models;

namespace SoundEffectLibrary.Data
{
    public class SfxDbContext : DbContext
    {
        public SfxDbContext(DbContextOptions<SfxDbContext> options) : base(options)
        {

        }

        public DbSet<AudioAsset> AudioAssets { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<AudioFile> AudioFiles { get; set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AudioAsset>(builder =>
            {
                builder.ToTable("audioassets", "assets");
                builder.HasKey(a => a.Id);

                builder.HasOne(a => a.Category)
                    .WithMany()
                    .HasForeignKey(a => a.CategoryId);

                builder.HasMany(a => a.AudioFiles)
                    .WithOne(f => f.AudioAsset)
                    .HasForeignKey(a => a.AssetId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Category>(builder =>
            {
                builder.ToTable("categories");
                builder.HasKey(c => c.Id);
            });

            modelBuilder.Entity<AudioFile>(builder =>
            {
                builder.ToTable("audiofiles", "assets");
                builder.HasKey(f => f.Id);
            });
        }
    }
}
