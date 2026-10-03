using Microsoft.EntityFrameworkCore;
using SoundEffectLibrary.Api.Data;
using SoundEffectLibrary.Api.Interface;

namespace SoundEffectLibrary.Api.Services
{
    public class GetAudioFileService
    {
        private readonly IAudioFileStorage _fileStorage;
        private readonly SfxDbContext _dbContext;
        public GetAudioFileService(
            IAudioFileStorage fileStorage,
            SfxDbContext dbContext)
        {
            _fileStorage = fileStorage;
            _dbContext = dbContext;
        }

        public async Task<GetAudioFileResult> GetAudioFile(Guid assetId, CancellationToken cancellationToken)
        {
            var asset = await _dbContext.AudioAssets
                                    .AsNoTracking()
                                    .Include(a => a.AudioFiles)
                                    .FirstOrDefaultAsync(a => a.Id == assetId);

            if (asset is null)
                return GetFailed($"Asset with ID:{assetId} not found.", GetAudioFileFailureType.AssetNotFound); // No asset with ID founded

            var file = asset.AudioFiles.FirstOrDefault();
            if (file is null)
                return GetFailed($"No file found in {asset.Title} asset.", GetAudioFileFailureType.FileNotFound); // Asset has no files (Should not happen by design)

            var stream = await _fileStorage.GetAsync(file.StorageKey, cancellationToken);
            if (stream is null)
                return GetFailed($"Storage  error", GetAudioFileFailureType.StorageError); // Storage failed to get file

            return GetSuccess(stream, file.FileName, file.ContentType);
        }

        private GetAudioFileResult GetSuccess(Stream stream, string fileName, string contentType)
        {
            return new GetAudioFileResult(true, null, null, stream,fileName, contentType);
        }
        private GetAudioFileResult GetFailed(string errorMessage, GetAudioFileFailureType failureType)
        {
            return new GetAudioFileResult(false, errorMessage, failureType, null, null, null);
        }
    }

    public record GetAudioFileResult(bool Success, string? ErrorMessage, GetAudioFileFailureType? FailureType, Stream? Stream, string? FileName, string? ContentType);

    public enum GetAudioFileFailureType
    {
        AssetNotFound = 0,
        FileNotFound = 1,
        StorageError = 2,
    }
}
