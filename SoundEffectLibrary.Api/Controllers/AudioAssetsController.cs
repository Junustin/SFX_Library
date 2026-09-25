using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SoundEffectLibrary.Api.Data;
using SoundEffectLibrary.Api.Models;
using SoundEffectLibrary.Api.Services;
using System.Security.Claims;

namespace SoundEffectLibrary.Api.Controllers                  
{
    [ApiController]
    [Route("api/audioassets")]
    [EnableRateLimiting("per-ip")]
    public class AudioAssetsController : ControllerBase
    {
        public AudioAssetsController() { }
        
        [HttpPost]
        [Authorize(Roles = "AssetManager")]
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
        [EnableRateLimiting("per-ip")]
        public async Task<ActionResult<GetAudioAssetRespose>> GetAudioAssets(
            SfxDbContext dbContext,
            [FromQuery] GetAudioAssetRequest request) 
        {
            IQueryable<AudioAsset> audioAssets = dbContext.AudioAssets
                .AsNoTracking()
                .Include(a => a.Category);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                audioAssets = audioAssets.Where(a => 
                    EF.Functions.ILike(a.Title, $"%{request.Search}%") 
                    || EF.Functions.ILike(a.Category.CategoryName, $"%{request.Search}%"));
            }

            audioAssets = audioAssets
                   .OrderBy(a => a.Category.CategoryName)
                   .ThenBy(a => a.Title)
                   .ThenBy(a => a.Id);

            var totalCount = await audioAssets.CountAsync();

            var skip = (request.Page - 1) * request.PageSize;
           
            var filteredAssets = await audioAssets
                    .Skip(skip)
                    .Take(request.PageSize)
                    .ToListAsync();

            
            var items = new List<AudioAssetCardResponse>();

            foreach (var asset in filteredAssets)
            {
                items.Add(new AudioAssetCardResponse
                (
                    asset.Id,
                    asset.Title,
                    new CategoryResponse(
                        asset.CategoryId,
                        asset.Category.CategoryName),
                    $"/api/audioassets/{asset.Id}/preview"
                ));
            }
            var response = new GetAudioAssetRespose(items, request.Page, request.PageSize, totalCount);

            return Ok(response);
        }

        [HttpGet("{id}")]
        [EnableRateLimiting("fixed")]
        public async Task<ActionResult<AudioAsset>> GetById(
            Guid id,
            SfxDbContext dbContext)
        {
            var audioAsset = await dbContext.AudioAssets
                .AsNoTracking()
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (audioAsset == null)
            {
                return Problem(
                    title: "Asset not found",
                    detail: "The requested audio asset was not found.",
                    statusCode: 404);
            }

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

        [HttpDelete("{id}")]
        [EnableRateLimiting("per-ip")]
        public async Task<ActionResult<AudioAsset>> DeleteAsset(
            Guid id,
            DeleteAudioAssetService deleteAudioAssetService,
            CancellationToken cancellationToken)
        {
            var deleteResult = await deleteAudioAssetService.DeleteAudioAssetAsync(id, cancellationToken);

            if (deleteResult.Success)
                return NoContent();

            switch (deleteResult.FailureType)
            {
                case DeleteAssetFailureType.AssetNotFound:
                    return NotFound();
                case DeleteAssetFailureType.FileNotFound:
                    return Problem(
                        detail: "File not found."
                        );
                case DeleteAssetFailureType.DeleteRecordFailed:
                    return Problem(
                        detail: "Delete record failed."
                        );
                case DeleteAssetFailureType.DeleteFileFailed:
                    return Problem(
                        detail: "Delete file failed."
                        );
                default:
                    return BadRequest();
            }
        }

        [HttpGet("test-error")]
        [DisableRateLimiting]
        public IActionResult TestError()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            throw new Exception("This should never be exposed to the client.");
        }
    }
}
