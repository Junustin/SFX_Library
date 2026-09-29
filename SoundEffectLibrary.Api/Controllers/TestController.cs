using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SoundEffectLibrary.Api.Data;
using SoundEffectLibrary.Api.Interface;
using SoundEffectLibrary.Api.Models;
using System.Security.Claims;

namespace SoundEffectLibrary.Api.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        [HttpGet("test-error")]
        [DisableRateLimiting]
        public IActionResult TestError()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            throw new Exception("This should never be exposed to the client.");
        }

        [HttpGet("cache-test")]
        public async Task<IActionResult> CacheTest(int testNum, ICacheService cache)
        {
            string key = $"sfx:cache-test{testNum}";

            var cachedValue = await cache.GetAsync<CacheTestResponse>(key);

            if (cachedValue is not null)
            {
                return Ok(new
                {
                    Source = "Cache",
                    Value = cachedValue
                });
            }

            var value = new CacheTestResponse(
                "Hello from cache",
                 DateTime.UtcNow
                );

            await cache.SetAsync(
                key,
                value,
                TimeSpan.FromMinutes(5));

            return Ok(new
            {
                Source = "Generated",
                Value = value
            });
        }

        [HttpPost("cache-group-delete")]
        public IActionResult CacheDeleteTest(ICacheService cache)
        {
            cache.RemoveByPrefixAsync("sfx:");
            return NoContent();
        }
    }
    public record CacheTestResponse(
    string Message,
    DateTime CreatedAt);
}
