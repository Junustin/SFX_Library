
using System.Diagnostics;
using Testcontainers.PostgreSql;

namespace SoundEffectLibrary.Api.Tests
{
    public class TestDatabaseFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
        public string ConnectionString => _postgres.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }
    }
}
