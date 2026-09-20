using Microsoft.AspNetCore.Mvc.Testing;
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
        public async Task AudioAssetsEndpoint_SearchNotInclude_ReturnPaginationJson()
        {
            // Arrange
            await _factory.ResetDatabaseAsync();
            await _factory.SeedDataAsync();

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/audioassets?page=1&pageSize=20");
            _output.WriteLine(await response.Content.ReadAsStringAsync());

            // Assert
            response.EnsureSuccessStatusCode();
        }
    }
}
