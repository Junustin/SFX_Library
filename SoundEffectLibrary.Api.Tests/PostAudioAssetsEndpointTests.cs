using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SoundEffectLibrary.Api.Data;
using SoundEffectLibrary.Api.Interface;
using SoundEffectLibrary.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Xunit.Abstractions;

namespace SoundEffectLibrary.Api.Tests
{
    public class PostAudioAssetsEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IClassFixture<TestDatabaseFixture>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;

        public PostAudioAssetsEndpointTests(TestDatabaseFixture database, ITestOutputHelper output)
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
        public async Task PostAudioAssets_UnAuthenticate_ReturnUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsync("/api/audioassets", null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PostAudioAssets_AuthenticateWithoutRole_ReturnForbidden()
        {
            // Arrange
            var client = _factory.CreateClient();

            using var scope = _factory.Services.CreateScope();
            var token = CreateTestToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.PostAsync("/api/audioassets", null);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostAudioAssets_AuthenticateWithRole_PassAuthorization()
        {
            // Arrange
            var client = _factory.CreateClient();

            using var scope = _factory.Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.PostAsync("/api/audioassets", null);

            // Assert
            // BadRequest is expected because this test only verifies that
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); 
        }

        [Fact]
        public async Task PostAudioAssets_WavHeaderFile_ReturnCreated()
        {
            // Arrange
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
           

            var tempFilePath = Path.Combine(
                Path.GetTempPath(),
                $"{ Guid.NewGuid()}.wav");
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
                // Act
                var response = await client.PostAsync("/api/audioassets", content);

                // Assert
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);

                // Check response content is correct
                var createdResponse = await response.Content.ReadFromJsonAsync<CreateAudioAssetResponse>();
                Assert.NotNull(createdResponse);
                Assert.NotEqual(Guid.Empty, createdResponse.Id);

                AudioAsset? audioAssets = 
                    await dbContext.AudioAssets
                    .AsNoTracking()
                    .Where(a => a.Id == createdResponse!.Id)
                    .Include(a => a.AudioFiles)
                    .Include(a => a.Category)
                    .SingleOrDefaultAsync();

                // Assets record get created
                Assert.NotNull(audioAssets);

                AudioFile audioFile = audioAssets.AudioFiles.Single();
                // Assets contain file
                Assert.Single(audioAssets.AudioFiles);
                // Database has file record
                Assert.Equal("test.wav", audioFile.FileName);

                Assert.NotNull(audioAssets.Category);
                Assert.Equal("FootStep", audioAssets.Category.CategoryName);

                var filePath = Path.Combine(_factory.TestStoragePath, audioFile.StorageKey);
                var extension = Path.GetExtension(filePath);
                Assert.True(File.Exists(filePath) && extension == ".wav");       
            }
            finally
            {
                // Clean up
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }  
        }

        [Fact]
        public async Task PostAudioAssets_NotWavHeader_ReturnBadRequest()
        {
            // Arrange
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


            var tempFilePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.wav");
            try
            {
                // Write file into temp path
                await File.WriteAllBytesAsync(tempFilePath, new byte[] { 1, 2, 3, 4, 5, 6 });

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
                // Act
                var response = await client.PostAsync("/api/audioassets", content);

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
            finally
            {
                // Clean up
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
        [Fact]
        public async Task PostAudioAssets_ValidMp3File_ReturnCreated()
        {
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            // Arrange
            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().GetToken();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var mp3Bytes = new byte[]
            {
                0xFF, 0xFB
            };

            // Get dbContext
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SfxDbContext>();

            int categoryId = await dbContext.Categories
                .Where(c => c.CategoryName == "FootStep")
                .Select(c => c.Id)
                .SingleAsync();

            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(mp3Bytes);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("audio/mpeg");

            content.Add(fileContent, "File", "test.mp3");
            content.Add(new StringContent("Test MP3"), "Title");
            content.Add(new StringContent("Test MP3 description"), "Description");
            content.Add(new StringContent(categoryId.ToString()), "CategoryId");

            // Act
            var response = await client.PostAsync(
                "/api/audioassets",
                content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
        [Fact]
        public async Task PostAudioAssets_InvalidMp3Signature_ReturnBadRequest()
        {
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            // Arrange
            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().GetToken();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var mp3Bytes = new byte[]
            {
                0x00, 0x00
            };

            // Get dbContext
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SfxDbContext>();

            int categoryId = await dbContext.Categories
                .Where(c => c.CategoryName == "FootStep")
                .Select(c => c.Id)
                .SingleAsync();

            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(mp3Bytes);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("audio/mpeg");

            content.Add(fileContent, "File", "test.mp3");
            content.Add(new StringContent("Test MP3"), "Title");
            content.Add(new StringContent("Test MP3 description"), "Description");
            content.Add(new StringContent(categoryId.ToString()), "CategoryId");

            // Act
            var response = await client.PostAsync(
                "/api/audioassets",
                content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        [Fact]
        public async Task PostAudioAssets_Mp3WithReservedVersion_ReturnBadRequest()
        {
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            // Arrange
            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().GetToken();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var mp3Bytes = new byte[]
            {
                0xFF, 0xEB
            };

            // Get dbContext
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SfxDbContext>();

            int categoryId = await dbContext.Categories
                .Where(c => c.CategoryName == "FootStep")
                .Select(c => c.Id)
                .SingleAsync();

            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(mp3Bytes);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("audio/mpeg");

            content.Add(fileContent, "File", "test.mp3");
            content.Add(new StringContent("Test MP3"), "Title");
            content.Add(new StringContent("Test MP3 description"), "Description");
            content.Add(new StringContent(categoryId.ToString()), "CategoryId");

            // Act
            var response = await client.PostAsync(
                "/api/audioassets",
                content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        [Fact]
        public async Task PostAudioAssets_Mp3WithReservedLayer_ReturnBadRequest()
        {
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            // Arrange
            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().GetToken();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var mp3Bytes = new byte[]
            {
                0xFF, 0xE1
            };

            // Get dbContext
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SfxDbContext>();

            int categoryId = await dbContext.Categories
                .Where(c => c.CategoryName == "FootStep")
                .Select(c => c.Id)
                .SingleAsync();

            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(mp3Bytes);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("audio/mpeg");

            content.Add(fileContent, "File", "test.mp3");
            content.Add(new StringContent("Test MP3"), "Title");
            content.Add(new StringContent("Test MP3 description"), "Description");
            content.Add(new StringContent(categoryId.ToString()), "CategoryId");

            // Act
            var response = await client.PostAsync(
                "/api/audioassets",
                content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        [Fact]
        public async Task PostAudioAssets_Mp3WithId3v2Tag_ReturnCreated()
        {
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            // Arrange
            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().GetToken();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var mp3Bytes = new byte[]
            {
                // ID3v2 header
                0x49, 0x44, 0x33, // "ID3"
                0x04,              // Version 2.4
                0x00,              // Revision
                0x00,              // Flags
                0x00, 0x00, 0x00, 0x00, // Tag size = 0

                // MPEG frame header
                0xFF, 0xFB
            };

            // Get dbContext
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SfxDbContext>();

            int categoryId = await dbContext.Categories
                .Where(c => c.CategoryName == "FootStep")
                .Select(c => c.Id)
                .SingleAsync();

            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(mp3Bytes);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("audio/mpeg");

            content.Add(fileContent, "File", "test.mp3");
            content.Add(new StringContent("Test MP3"), "Title");
            content.Add(new StringContent("Test MP3 description"), "Description");
            content.Add(new StringContent(categoryId.ToString()), "CategoryId");

            // Act
            var response = await client.PostAsync(
                "/api/audioassets",
                content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
        [Fact]
        public async Task PostAudioAssets_Mp3WithInvalidId3v2Size_ReturnBadRequest()
        {
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            // Arrange
            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().GetToken();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var mp3Bytes = new byte[]
            {
                // ID3v2 header
                0x49, 0x44, 0x33, // "ID3"
                0x04,              // Version
                0x00,              // Revision
                0x00,              // Flags

                // Invalid synchsafe size
                0x80, 0x00, 0x00, 0x00,

                // MPEG frame header
                0xFF, 0xFB
            };

            // Get dbContext
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SfxDbContext>();

            int categoryId = await dbContext.Categories
                .Where(c => c.CategoryName == "FootStep")
                .Select(c => c.Id)
                .SingleAsync();

            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(mp3Bytes);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("audio/mpeg");

            content.Add(fileContent, "File", "test.mp3");
            content.Add(new StringContent("Test MP3"), "Title");
            content.Add(new StringContent("Test MP3 description"), "Description");
            content.Add(new StringContent(categoryId.ToString()), "CategoryId");

            // Act
            var response = await client.PostAsync(
                "/api/audioassets",
                content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private string CreateTestToken(string role = "")
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("this-is-my-secret-signingkey-for-using-in-development-it-is-not-the-real-key-so-dont-worry-about-it"));

            var credential = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "user-123"));

            if (!string.IsNullOrWhiteSpace(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: "library-auth",
                audience: "library-api",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: credential
                );

            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(token);
        }
    }
}
