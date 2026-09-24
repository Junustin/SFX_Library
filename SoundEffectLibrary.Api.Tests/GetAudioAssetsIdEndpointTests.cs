using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace SoundEffectLibrary.Api.Tests
{
    public class GetAudioAssetsIdEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IClassFixture<TestDatabaseFixture>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;

        public GetAudioAssetsIdEndpointTests(TestDatabaseFixture database, ITestOutputHelper output)
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

    }
}
