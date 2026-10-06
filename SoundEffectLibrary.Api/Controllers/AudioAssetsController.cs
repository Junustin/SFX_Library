using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SoundEffectLibrary.Api.Data;
using SoundEffectLibrary.Api.Dtos;
using SoundEffectLibrary.Api.Interface;
using SoundEffectLibrary.Api.Models;
using SoundEffectLibrary.Api.Services;

namespace SoundEffectLibrary.Api.Controllers                  
{
    [ApiController]
    [Route("api/audioassets")]
    [EnableRateLimiting("per-ip")]
    public class AudioAssetsController : ControllerBase
    {
        public const string AudioAssetsCachePrefix = "audioassets:";
        public AudioAssetsController() { }
        
        [HttpPost]
        [Authorize(Roles = "AssetManager")]
        [EnableRateLimiting("per-ip")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<AudioAsset>> AddAudio(
            [FromForm]CreateAudioAssetRequest request,
            CreateAudioAssetService createAudioAssetService,
            ICacheService cache,
            CancellationToken cancellationToken)
        {
            var createResult = await createAudioAssetService.CreateAudioAssetAsync(request, cancellationToken);

            if (!createResult.Success)
                return BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = createResult.ErrorMessage,
                    Status = 400
                });

            // Cache invalidation
            await cache.RemoveByPrefixAsync(AudioAssetsCachePrefix); 

            return CreatedAtAction(nameof(GetById), new { id = createResult.AudioAssetId }, createResult.response);
        }

        [HttpGet]
        [EnableRateLimiting("per-ip")]
        public async Task<ActionResult<GetAudioAssetResponse>> GetAudioAssets(
            SearchAudioAssetPaginationService searchAudioAssetPaginationService,
            ICacheService cache,
            [FromQuery] GetAudioAssetRequest request) 
        {
            var cacheKey = $"{AudioAssetsCachePrefix}search={request.Search}:page={request.Page}:pagesize={request.PageSize}";

            var cached = await cache.GetAsync<GetAudioAssetResponse>(cacheKey);

            if(cached is not null)
            {
                return Ok(cached);
            }

            var result = await searchAudioAssetPaginationService.GetAudioAssets(request);

            var filteredAssets = result.FilteredAssets;
            var totalCount = result.TotalCount;
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
            var response = new GetAudioAssetResponse(items, request.Page, request.PageSize, totalCount);

            // Put response in cache
            await cache.SetAsync(cacheKey, response, TimeSpan.FromSeconds(20));

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
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = "Asset not found.",
                    Status = 404
                });
            }

            return Ok(audioAsset);
        }

        [HttpGet("{id}/files")] // For download file
        [EnableRateLimiting("per-ip")]
        public async Task<ActionResult> GetFile(
            Guid id,
            CancellationToken cancellationToken,
            GetAudioFileService getAudioFileService)
        {
            var result = await getAudioFileService.GetAudioFile(id, cancellationToken);

            if (result.Success)
                return File(result.Stream!, result.ContentType!, result.FileName!);

             if (result.FailureType is null) // Should not happen but just in case.
                return Problem(
                    title: "Internal server error",
                    statusCode: 500
                    );

            switch (result.FailureType)
            {
                case GetAudioFileFailureType.AssetNotFound:
                    return NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = "Asset not found.",
                        Status = 404
                    });
                case GetAudioFileFailureType.FileNotFound:
                    return NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = "File not found.",
                        Status = 404
                    }); 
                case GetAudioFileFailureType.StorageError :
                    return Problem(
                        title: "Internal server error",
                        statusCode: 500
                        );
                default: 
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Request is invalid.",
                        Status = 400
                    });
             }
         }

        [HttpGet("{id}/preview")] // Used for preview audio file
        [EnableRateLimiting("per-ip")]
        public async Task<ActionResult> GetCardPreview(
            Guid id,
            CancellationToken cancellationToken,
            GetAudioFileService getAudioFileService)
        {
            var result = await getAudioFileService.GetAudioFile(id, cancellationToken);

            if (result.Success)
                return File(result.Stream!, result.ContentType!);

            if (result.FailureType is null) // Should not happen but just in case.
                return Problem(
                    title: "Internal server error",
                    statusCode: 500
                    );

            switch (result.FailureType)
            {
                case GetAudioFileFailureType.AssetNotFound:
                    return NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = "Asset not found.",
                        Status = 404
                    });
                case GetAudioFileFailureType.FileNotFound:
                    return NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = "File not found.",
                        Status = 404
                    });
                case GetAudioFileFailureType.StorageError:
                    return Problem(
                        title: "Internal server error.",
                        statusCode: 500
                        );         
                default:
                    return BadRequest(new ProblemDetails
                    {
                        Title = "BadRequest",
                        Detail = "Request is invalid.",
                        Status = 400
                    });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "AssetManager")]
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
                    return NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = "Asset not found.",
                        Status = 404
                    });
                case DeleteAssetFailureType.FileNotFound:
                    return NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = "File not found.",
                        Status = 404
                    });
                case DeleteAssetFailureType.DeleteRecordFailed:
                    return NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = "Delete record failed.",
                        Status = 404
                    });
                case DeleteAssetFailureType.DeleteFileFailed:
                    return NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = "Delete file failed.",
                        Status = 404
                    });
                default:
                    return BadRequest(new ProblemDetails
                    {
                        Title = "BadRequest",
                        Detail = "Invalid request.",
                        Status = 400
                    });
            }
        }  
    }
}
