using Microsoft.AspNetCore.Mvc.Testing;
using SoundEffectLibrary.Api.Models;
using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

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
    }
}
