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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AudioAsset>(builder =>
            {
                builder.ToTable("audioassets", "assets");
                builder.HasKey(a => a.Id);

                builder.HasOne<Category>()
                    .WithMany()
                    .HasForeignKey(c => c.CategoryId);
            });

            modelBuilder.Entity<Category>(builder =>
            {
                builder.ToTable("categories");
                builder.HasKey(c => c.Id);
            });
        }
    }
}
