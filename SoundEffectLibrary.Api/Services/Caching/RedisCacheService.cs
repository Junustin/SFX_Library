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
            try
            {
                var database = _redis.GetDatabase();

                var json = JsonSerializer.Serialize(value);

                await database.StringSetAsync(key, json, expiration);
            }
            catch(RedisConnectionException)
            {
                // Connection failure should not fail the request.
            }
        }
        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var database = _redis.GetDatabase();

                var value = await database.StringGetAsync(key);

                if (value.IsNullOrEmpty)
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(value.ToString()!);
            }
            catch(RedisConnectionException ex)
            {
                Console.WriteLine($"Redis failed: {ex.Message}");
                return default; // Treate as cache miss
            }
        }
        public async Task RemoveAsync(string key)
        {
            try
            {
                var database = _redis.GetDatabase();

                await database.KeyDeleteAsync(key);
            }
            catch (RedisConnectionException)
            {
                // Connection failure should not fail the request.
            }
            
        }
        public async Task RemoveByPrefixAsync(string prefix)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints()[0]);

                var keys = server.Keys(pattern: $"{prefix}*");

                var database = _redis.GetDatabase();

                foreach (var key in keys)
                {
                    await database.KeyDeleteAsync(key);
                }
            }
            catch (RedisConnectionException)
            {
                // Connection failure should not fail the request.
            }

        }
    }
}