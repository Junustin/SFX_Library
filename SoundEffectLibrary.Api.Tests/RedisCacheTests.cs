using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoundEffectLibrary.Api.Controllers;
using SoundEffectLibrary.Api.Data;
using SoundEffectLibrary.Api.Interface;
using SoundEffectLibrary.Api.Models;
using System.Net;
using System.Net.Http.Headers;
using Xunit.Abstractions;

namespace SoundEffectLibrary.Api.Tests
{
    public class RedisCacheTests : IClassFixture<WebApplicationFactory<Program>>, IClassFixture<TestDatabaseFixture>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;

        public RedisCacheTests(TestDatabaseFixture database, ITestOutputHelper output)
        {
            _factory = new CustomWebApplicationFactory(database);
            _output = output;
        }

        public async Task InitializeAsync()
        {
            await _factory.InitializeDatabaseAsync();
        }
        public Task DisposeAsync()
        {
            _factory.Dispose();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task RedisCache_RemoveByPrefixAsync_RemoveMatchingKeys()
        {
            // Arrange
            var cache = _factory.Services.GetRequiredService<ICacheService>();

            await cache.SetAsync(
                "audioassets:test:1",
                "value:1",
                TimeSpan.FromMinutes(5)
                );

            await cache.SetAsync(
                "audioassets:test:2",
                "value:2",
                TimeSpan.FromMinutes(5)
                );

            await cache.SetAsync(
                "audioassets:test:3",
                "value:3",
                TimeSpan.FromMinutes(5)
                );

            await cache.SetAsync(
                "user:test:1",
                "user value",
                TimeSpan.FromMinutes(5)
                );

            // Act
            await cache.RemoveByPrefixAsync("audioassets:");

            // Assert
            Assert.Null(await cache.GetAsync<string>("audioassets:test:1"));
            Assert.Null(await cache.GetAsync<string>("audioassets:test:2"));
            Assert.Null(await cache.GetAsync<string>("audioassets:test:3"));

            Assert.Equal("user value", await cache.GetAsync<string>("user:test:1"));
        }

        [Fact]
        public async Task RedisCache_PostSucceed_RemoveCacheWithMatchingPrefix()
        {
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            var client = _factory.CreateClient();
            var cache = _factory.Services.GetRequiredService<ICacheService>();
            string audioAssetsCachePrefix = AudioAssetsController.AudioAssetsCachePrefix;

            using var scope = _factory.Services.CreateScope();

            var token = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 1stGet set first cache key
            var response1 = await client.GetAsync("/api/audioassets?page=1&pageSize=2");
            // 2ndGet for second cache key
            var response2 = await client.GetAsync("/api/audioassets?page=2&pageSize=2");

            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

            // Set random cache key
            await cache.SetAsync(
                "user:test1",
                "user value",
                TimeSpan.FromMinutes(5)
                );
            // Assert that both cache exist before post
            Assert.NotNull(await cache.GetAsync<GetAudioAssetResponse>($"{audioAssetsCachePrefix}search=:page=1:pagesize=2"));
            Assert.NotNull(await cache.GetAsync<GetAudioAssetResponse>($"{audioAssetsCachePrefix}search=:page=2:pagesize=2"));

            // Post new audio file
            var tempFilePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.wav");
            try
            {
                // Write file into temp path
                await File.WriteAllBytesAsync(tempFilePath, new byte[]
                    {
                        0x52, 0x49, 0x46, 0x46, // RIFF
                        0x00, 0x00, 0x00, 0x00, // file size placeholder
                        0x57, 0x41, 0x56, 0x45  // WAVE
                    });

                // Get stream from created file
                using var fileStream = File.OpenRead(tempFilePath);
                // Turn stream into content
                using var fileContent = new StreamContent(fileStream);
                // Create multiform 
                using var content = new MultipartFormDataContent();
                // Set content headers
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

                // Get dbContext
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<SfxDbContext>();

                int categoryId = await dbContext.Categories
                    .Where(c => c.CategoryName == "FootStep")
                    .Select(c => c.Id)
                    .SingleAsync();

                content.Add(new StringContent("Test Audio"), "Title");
                content.Add(new StringContent("Test Description"), "Description");
                content.Add(new StringContent(categoryId.ToString()), "CategoryId");
                content.Add(fileContent, "File", "test.wav");

                var response = await client.PostAsync("/api/audioassets", content);

                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            }
            finally
            {
                // Clean up
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }

            // Assert Get prefix cache don't exist after post succeed
            Assert.Null(await cache.GetAsync<GetAudioAssetResponse>($"{audioAssetsCachePrefix}search=:page=1:pagesize=2"));
            Assert.Null(await cache.GetAsync<GetAudioAssetResponse>($"{audioAssetsCachePrefix}search=:page=2:pagesize=2"));
            // Assert random cache key still presist
            Assert.NotNull(await cache.GetAsync<string>("user:test1"));
        }

        [Fact]
        public async Task RedisCache_PostFailed_CacheWithMatchingPrefixStillPresist()
        {
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            var client = _factory.CreateClient();
            var cache = _factory.Services.GetRequiredService<ICacheService>();
            string audioAssetsCachePrefix = AudioAssetsController.AudioAssetsCachePrefix;

            using var scope = _factory.Services.CreateScope();

            var token = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 1stGet set first cache key
            var response1 = await client.GetAsync("/api/audioassets?page=1&pageSize=2");
            // 2ndGet for second cache key
            var response2 = await client.GetAsync("/api/audioassets?page=2&pageSize=2");

            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

            // Set random cache key
            await cache.SetAsync(
                "user:test1",
                "user value",
                TimeSpan.FromMinutes(5)
                );
            // Assert that both cache exist before post
            Assert.NotNull(await cache.GetAsync<GetAudioAssetResponse>($"{audioAssetsCachePrefix}search=:page=1:pagesize=2"));
            Assert.NotNull(await cache.GetAsync<GetAudioAssetResponse>($"{audioAssetsCachePrefix}search=:page=2:pagesize=2"));

            // Post new audio file failed
            var response = await client.PostAsync("/api/audioassets", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // Assert Get prefix cache still exist after post failed
            Assert.NotNull(await cache.GetAsync<GetAudioAssetResponse>($"{audioAssetsCachePrefix}search=:page=1:pagesize=2"));
            Assert.NotNull(await cache.GetAsync<GetAudioAssetResponse>($"{audioAssetsCachePrefix}search=:page=2:pagesize=2"));
            // Assert random cache key still presist
            Assert.NotNull(await cache.GetAsync<string>("user:test1"));
        }
    }
}
