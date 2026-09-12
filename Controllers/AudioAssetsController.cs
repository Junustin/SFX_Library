using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SoundEffectLibrary.Data;
using SoundEffectLibrary.Interface;
using SoundEffectLibrary.Models;
using SoundEffectLibrary.Services;

namespace SoundEffectLibrary.Controllers
{
    [ApiController]
    [Route("api/sfx")]
    public class AudioAssetsController : ControllerBase
    {
        public AudioAssetsController() { }
        
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<AudioAsset>> AddAudio(
            [FromForm]CreateAudioAssetRequest request,
            CreateAudioAssetService createAudioAssetService,
            CancellationToken cancellationToken)
        {
            var createResult = await createAudioAssetService.CreateAudioAssetAsync(request, cancellationToken);

            if (!createResult.Success)
                return BadRequest(createResult.ErrorMessage);

            return CreatedAtAction(nameof(GetById), new { id = createResult.AudioAssetId }, createResult.response);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AudioAsset>>> GetAudioAssets(
            SfxDbContext dbContext)
        {
            var audioAssets = await dbContext.AudioAssets
                .AsNoTracking()
                .Include(a => a.Category)
                .ToListAsync();

            return Ok(audioAssets);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AudioAsset>> GetById(
            Guid id,
            SfxDbContext dbContext)
        {
            var audioAsset = await dbContext.AudioAssets
                .AsNoTracking()
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (audioAsset == null)
                return NotFound($"No audio asset with this ID:{id} found.");

            return Ok(audioAsset);
        }
    }
}
