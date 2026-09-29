using SoundEffectLibrary.Api.Interface;
using StackExchange.Redis;
using System.Text.Json;

namespace SoundEffectLibrary.Api.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }
        public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            var database = _redis.GetDatabase();

            var json = JsonSerializer.Serialize(value);

            await database.StringSetAsync(key, json, expiration);
        }
        public async Task<T?> GetAsync<T>(string key)
        {
            var database = _redis.GetDatabase();

            var value = await database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value.ToString()!);
        }
        public async Task RemoveAsync(string key)
        {
            var database = _redis.GetDatabase();

            await database.KeyDeleteAsync(key);
        }

        public async Task RemoveByPrefixAsync(string prefix)
        {
            var server = _redis.GetServer(_redis.GetEndPoints()[0]);

            var keys = server.Keys(pattern: $"{prefix}*");

            var database = _redis.GetDatabase();

            foreach(var key in keys)
            {
                await database.KeyDeleteAsync(key);
            }
        }
    }
}
