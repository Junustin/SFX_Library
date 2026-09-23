using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Http.Features;
using SoundEffectLibrary.Api.Models;
using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;
using Org.BouncyCastle.Tls;

namespace SoundEffectLibrary.Api.Tests
{
    public class AudioAssetsEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IClassFixture<TestDatabaseFixture>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;

        public AudioAssetsEndpointTests(TestDatabaseFixture database, ITestOutputHelper output)
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
        public async Task GetAudioAssets_FirstPage_ReturnsFirstPageResult()
        {
            // Arrange
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/audioassets?page=1&pageSize=2");
            _output.WriteLine(await response.Content.ReadAsStringAsync());

            // Assert
            response.EnsureSuccessStatusCode();
            GetAudioAssetRespose? assets = await response.Content.ReadFromJsonAsync<GetAudioAssetRespose>();
            Assert.Multiple(() =>
            {
                Assert.Equal(1, assets!.Page);
                Assert.Equal(2, assets.PageSize);
                Assert.Equal(2, assets.Items.Count);
                Assert.Equal(5, assets.TotalCount);

                Assert.Equal("Forest Wind", assets.Items[0].Title);
                Assert.Equal("Metal Footsteps", assets.Items[1].Title);
            });
        }

        [Fact]
        public async Task GetAudioAssets_SecondPage_ReturnsSecondPageResult()
        {
            // Arrange
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/audioassets?page=2&pageSize=2");
            _output.WriteLine(await response.Content.ReadAsStringAsync());

            // Assert
            response.EnsureSuccessStatusCode();
            GetAudioAssetRespose? assets = await response.Content.ReadFromJsonAsync<GetAudioAssetRespose>();
            Assert.Multiple(() =>
            {
                Assert.Equal(2, assets!.Page);
                Assert.Equal(2, assets.PageSize);
                Assert.Equal(2, assets.Items.Count);
                Assert.Equal(5, assets.TotalCount);

                Assert.Equal("Wooden Footsteps", assets.Items[0].Title);
                Assert.Equal("Sword Impact", assets.Items[1].Title);
            });
        }

        [Fact]
        public async Task GetAudioAssets_LastPage_ReturnsRemainingResult()
        {
            // Arrange
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/audioassets?page=2&pageSize=3");
            _output.WriteLine(await response.Content.ReadAsStringAsync());

            // Assert
            response.EnsureSuccessStatusCode();
            GetAudioAssetRespose? assets = await response.Content.ReadFromJsonAsync<GetAudioAssetRespose>();
            Assert.Multiple(() =>
            {
                Assert.Equal(2, assets!.Page);
                Assert.Equal(3, assets.PageSize);
                Assert.Equal(2, assets.Items.Count);
                Assert.Equal(5, assets.TotalCount);
            });
        }

        [Fact]
        public async Task GetAudioAssets_PageBeyondResults_ReturnEmptyItems()
        {
            // Arrange
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/audioassets?page=3&pageSize=3");
            _output.WriteLine(await response.Content.ReadAsStringAsync());

            // Assert
            response.EnsureSuccessStatusCode();
            GetAudioAssetRespose? assets = await response.Content.ReadFromJsonAsync<GetAudioAssetRespose>();
            Assert.Multiple(() =>
            {
                Assert.Empty(assets!.Items);
                Assert.Equal(5, assets.TotalCount);
            });
        }

        [Fact]
        public async Task GetAudioAssets_SearchMatch_ReturnsMatchingResults()
        {
            // Arrange
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/audioassets?search=Footsteps&page=1&pageSize=5");
            _output.WriteLine(await response.Content.ReadAsStringAsync());

            // Assert
            response.EnsureSuccessStatusCode();
            GetAudioAssetRespose? assets = await response.Content.ReadFromJsonAsync<GetAudioAssetRespose>();
            Assert.Multiple(() =>
            {
                Assert.Equal(2, assets!.Items.Count);
                Assert.Equal(2, assets.TotalCount);

                Assert.All(
                assets.Items,
                item => Assert.True(
                    item.Title.Contains("Footsteps", StringComparison.OrdinalIgnoreCase) ||
                    item.CategoryResponse.CategoryName.Contains("Footsteps", StringComparison.OrdinalIgnoreCase)));
            });
        }

        [Fact]
        public async Task GetAudioAssets_SearchAllUppercase_ReturnsCaseInsensitiveMatchesResult()
        {
            // Arrange
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/audioassets?search=FOOTSTEPS&page=1&pageSize=5");
            _output.WriteLine(await response.Content.ReadAsStringAsync());

            // Assert
            response.EnsureSuccessStatusCode();
            GetAudioAssetRespose? assets = await response.Content.ReadFromJsonAsync<GetAudioAssetRespose>();
            Assert.Multiple(() =>
            {
                Assert.Equal(2, assets!.Items.Count);
                Assert.Equal(2, assets.TotalCount);

                Assert.All(
                assets.Items,
                item => Assert.True(
                    item.Title.Contains("FOOTSTEPS", StringComparison.OrdinalIgnoreCase) ||
                    item.CategoryResponse.CategoryName.Contains("FOOTSTEPS", StringComparison.OrdinalIgnoreCase)));
            });
        }

        [Fact]
        public async Task GetAudioAssets_InvalidPageValue_ReturnsBadRequest()
        {
            // Arrange

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/audioassets?page=abc&pageSize=3");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetAudioAssets_InvalidPageSizeValue_ReturnBadRequest()
        {
            // Arrange

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/audioassets?page=3&pageSize=abc");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetAudioAssets_NoExistId_ReturnNotFoundRequest()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/audioassets/cd746347-a4f2-45aa-9975-8b4c9bf9e719");
            ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType!.ToString());
            Assert.Equal(404, problem!.Status);
            Assert.Equal("The requested audio asset was not found.", problem.Detail);
        }

        [Fact]
        public async Task GetAudioAssets_InvalidId_ReturnBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/audioassets/not-a-guid");
            ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            var errors = problem!.Extensions["errors"];

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType!.ToString());
            Assert.Contains("The value 'not-a-guid' is not valid.", errors!.ToString());
        }

        [Fact]
        public async Task GetError_TestError_ReturnServerError()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/audioassets/test-error");
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType!.ToString());
            Assert.DoesNotContain("This should never be exposed to the client.", body);
        }

        [Fact]
        public async Task GetAudioAssets_ExceedRateLimit_ReturnTooManyRequests()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            for (int i = 0; i < 5; i++)
            {
                var r = await client.GetAsync("/api/audioassets?page=1&pageSize=2");
                Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            }

            var response = await client.GetAsync("/api/audioassets?page=1&pageSize=2");

            // Assert
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }
}
