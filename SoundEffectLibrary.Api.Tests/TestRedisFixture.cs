using Testcontainers.Redis;

namespace SoundEffectLibrary.Api.Tests
{
    public class TestRedisFixture : IAsyncLifetime
    {
        private readonly RedisContainer _redis = new RedisBuilder("redis:8").Build();
        public string ConnectionString => _redis.GetConnectionString();
        public async Task InitializeAsync()
        {
            await _redis.StartAsync();
        }
        public async Task DisposeAsync()
        {
            await _redis.DisposeAsync();
        }
    }
}
