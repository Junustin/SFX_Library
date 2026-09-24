using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SoundEffectLibrary.Api.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
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
        public async Task PostAudioAssets_Authenticated_ReturnCreated()
        {
            // Arrange
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            var client = _factory.CreateClient();
            var token = CreateTestToken();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var tempFilePath = Path.Combine(
                Path.GetTempPath(),
                $"{ Guid.NewGuid()}.wav");
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
                using var scope = _factory.Services.CreateScope();
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

        private string CreateTestToken()
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("this-is-my-secret-signingkey-for-using-in-development-it-is-not-the-real-key-so-dont-worry-about-it"));

            var credential = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "library-auth",
                audience: "library-api",
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: credential
                );

            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(token);
        }
    }
}
