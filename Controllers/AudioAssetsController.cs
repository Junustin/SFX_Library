using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SoundEffectLibrary.Data;
using SoundEffectLibrary.Interface;
using SoundEffectLibrary.Models;

namespace SoundEffectLibrary.Controllers
{
    [ApiController]
    [Route("api/sfx")]
    public class AudioAssetsController : ControllerBase
    {
        private readonly IAudioFileValidator _fileValidator;
        public AudioAssetsController(IAudioFileValidator fileValidator) 
        {
            _fileValidator = fileValidator;
        }
        

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<AudioAsset>> AddAudio(
            [FromForm]CreateAudioAssetRequest request,
            SfxDbContext dbContext)
        {
            // Validate file
            var validateResult = _fileValidator.Validate(request.File);
            if (!validateResult.IsValid)
            {
                // Validation failed
                return BadRequest(validateResult.ErrorMessage);
            }
            
            var audioAsset = new AudioAsset
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                CategoryId = request.CategoryId
            };

            var audioFile = new AudioFile
            {
                Id= Guid.NewGuid(),
                AssetId = audioAsset.Id,
                FileName = request.File.FileName,
                ContentType = request.File.ContentType,
                FileSize = request.File.Length,
                // Create storage key
            };

            dbContext.AudioAssets.Add(audioAsset);
            await dbContext.SaveChangesAsync();

            // Create resonse Dto
            var responseDto = new CreateAudioAssetResponse
            (
                audioAsset.Id,
                audioAsset.Title,
                audioAsset.Description,
                audioAsset.CategoryId
            );

            return CreatedAtAction(nameof(GetById), new { id = audioAsset.Id }, responseDto);
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
