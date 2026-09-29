using Microsoft.EntityFrameworkCore;
using SoundEffectLibrary.Api.Data;
using SoundEffectLibrary.Api.Models;

namespace SoundEffectLibrary.Api.Services
{
    public class SearchAudioAssetPaginationService
    {
        private readonly SfxDbContext _dbContext;
        public SearchAudioAssetPaginationService(SfxDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<SearchAudioAssetPaginationResult> GetAudioAssets(GetAudioAssetRequest request)
        {
            IQueryable<AudioAsset> audioAssets = _dbContext.AudioAssets
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
            
            return new SearchAudioAssetPaginationResult(filteredAssets, totalCount);
        }

        public record SearchAudioAssetPaginationResult(List<AudioAsset> FilteredAssets, int TotalCount);
    }
}
