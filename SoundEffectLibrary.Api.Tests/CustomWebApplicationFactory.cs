using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoundEffectLibrary.Api.Data;
using SoundEffectLibrary.Api.Models;

namespace SoundEffectLibrary.Api.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly TestDatabaseFixture _database;
        public CustomWebApplicationFactory(TestDatabaseFixture database)
        {
            _database = database;
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Replace original database with test container database
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<SfxDbContext>();

                services.AddDbContext<SfxDbContext>(options =>
                    options.UseNpgsql(_database.ConnectionString));
            });
        }

        public async Task InitializeDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SfxDbContext>();

            await dbContext.Database.MigrateAsync();
        }
        public async Task SeedDataAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SfxDbContext>();

            var footsteps = new Category
            {
                CategoryName = "FootStep"
            };

            var weapons = new Category
            {
                CategoryName = "Weapons"
            };

            var environment = new Category
            {
                CategoryName = "Environment"
            };
            
            dbContext.Categories.AddRange(footsteps, weapons , environment);

            await dbContext.SaveChangesAsync();

            var woodenFootstepsId =
                Guid.Parse("10000000-0000-0000-0000-000000000001");

            var metalFootstepsId =
                Guid.Parse("10000000-0000-0000-0000-000000000002");

            var swordSwingId =
                Guid.Parse("10000000-0000-0000-0000-000000000003");

            var swordImpactId =
                Guid.Parse("10000000-0000-0000-0000-000000000004");

            var forestWindId =
                Guid.Parse("10000000-0000-0000-0000-000000000005");

            var assets = new[]
            {
                new AudioAsset
                {
                    Id = woodenFootstepsId,
                    Title = "Wooden Footsteps",
                    Description = "Footsteps on a wooden floor",
                    CategoryId = footsteps.Id
                },
                new AudioAsset
                {
                    Id = metalFootstepsId,
                    Title = "Metal Footsteps",
                    Description = "Footsteps on a metal surface",
                    CategoryId = footsteps.Id
                },
                new AudioAsset
                {
                    Id = swordSwingId,
                    Title = "Sword Swing",
                    Description = "A sword being swung",
                    CategoryId = weapons.Id
                },
                new AudioAsset
                {
                    Id = swordImpactId,
                    Title = "Sword Impact",
                    Description = "A sword impact sound",
                    CategoryId = weapons.Id
                },
                new AudioAsset
                {
                    Id = forestWindId,
                    Title = "Forest Wind",
                    Description = "Wind blowing through a forest",
                    CategoryId = environment.Id
                }
            };

            dbContext.AddRange(assets);

            await dbContext.SaveChangesAsync();
        }
        public async Task ResetDatabaseAsync()
        {
            using var scope = Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<SfxDbContext>();

            dbContext.AudioFiles.RemoveRange(dbContext.AudioFiles);
            dbContext.AudioAssets.RemoveRange(dbContext.AudioAssets);
            dbContext.Categories.RemoveRange(dbContext.Categories);

            await dbContext.SaveChangesAsync();
        }
    }
}
