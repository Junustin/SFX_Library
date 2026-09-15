using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SoundEffectLibrary.Api.Data;
using SoundEffectLibrary.Api.Interface;
using SoundEffectLibrary.Api.Models;
using SoundEffectLibrary.Api.Services;

namespace SoundEffectLibrary.Api.Controllers
{
    [ApiController]
    [Route("api/audioassets")]
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
        public async Task<ActionResult<IReadOnlyList<AudioAssetCardResponse>>> GetAudioAssets(
            SfxDbContext dbContext)
        {
            var audioAssets = await dbContext.AudioAssets
                .AsNoTracking()
                .Include(a => a.Category)
                .ToListAsync();

            var response = new List<AudioAssetCardResponse>();

            foreach (var asset in audioAssets)
            {
                response.Add(new AudioAssetCardResponse
                (
                    asset.Id,
                    asset.Title,
                    new CategoryResponse(
                        asset.CategoryId,
                        asset.Category.CategoryName),
                    $"/api/audioassets/{asset.Id}/preview"
                ));   
            }

            return Ok(response);
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

        [HttpGet("{id}/files")] // For download file
        public async Task<ActionResult> GetFile(
            Guid id,
            CancellationToken cancellationToken,
            GetAudioFileService getAudioFileService)
        {
            var result = await getAudioFileService.GetAudioFile(id, cancellationToken);

            if (result.Success)
                return File(result.Stream!, result.ContentType!, result.FileName!);

             if (result.FailureType is null) // Should not happen but just in case.
                return Problem();

             switch (result.FailureType)
             {
                case GetAudioFileFailureType.AssetNotFound :
                    return NotFound();
                case GetAudioFileFailureType.FileNotFound :
                    return Problem(); 
                case GetAudioFileFailureType.StorageError :
                    return Problem();
                default: 
                    return BadRequest();
             }
         }

        [HttpGet("{id}/preview")] // Used for preview audio file
        public async Task<ActionResult> GetCardPreview(
            Guid id,
            CancellationToken cancellationToken,
            GetAudioFileService getAudioFileService)
        {
            var result = await getAudioFileService.GetAudioFile(id, cancellationToken);

            if (result.Success)
                return File(result.Stream!, result.ContentType!);

            if (result.FailureType is null) // Should not happen but just in case.
                return Problem();

            switch (result.FailureType)
            {
                case GetAudioFileFailureType.AssetNotFound:
                    return NotFound();
                case GetAudioFileFailureType.FileNotFound:
                    return Problem();
                case GetAudioFileFailureType.StorageError:
                    return Problem();
                default:
                    return BadRequest();
            }
        }
    }
}
