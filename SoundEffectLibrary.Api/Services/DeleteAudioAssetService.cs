using Microsoft.EntityFrameworkCore;
using SoundEffectLibrary.Api.Data;
using SoundEffectLibrary.Api.Interface;
using SoundEffectLibrary.Api.Models;

namespace SoundEffectLibrary.Api.Services
{
    public class DeleteAudioAssetService
    {
        private readonly IAudioFileStorage _fileStorage;
        private readonly SfxDbContext _dbContext;
        public DeleteAudioAssetService(
            IAudioFileValidator fileValidator,
            IAudioFileStorage fileStorage,
            SfxDbContext dbContext)
        {
            _fileStorage = fileStorage;
            _dbContext = dbContext;
        }

        public async Task<DeleteAudioAssetResult> DeleteAudioAssetAsync(Guid assetId, CancellationToken cancellationToken)
        {
            // Get asset record
            AudioAsset? asset = await _dbContext.AudioAssets
                .AsNoTracking()
                .Include(a => a.AudioFiles)
                .SingleOrDefaultAsync(a => a.Id == assetId);

            if (asset is null)
                return DeleteFailed($"Asset with ID:{assetId} not found.", DeleteAssetFailureType.AssetNotFound);

            // Get storage key
            List<AudioFile> files = await _dbContext.AudioFiles
                .AsNoTracking()
                .Where(f => f.AssetId == assetId).ToListAsync();

            if (files.Count() <= 0)
            {
                return DeleteFailed($"Asset with ID:{assetId} has not file.", DeleteAssetFailureType.FileNotFound);
            }

            string storageKey = asset!.AudioFiles.Single().StorageKey;
            if(storageKey is null || !_fileStorage.IsAudioFileExist(storageKey))
                return DeleteFailed($"No storage key found.", DeleteAssetFailureType.FileNotFound);

            // Try delete record in database
            try
            {
                // Delete record from Database
                await _dbContext.AudioAssets
                    .Where(a => a.Id == assetId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            // Delete from database failed
            catch (Exception ex)
            {          
                return DeleteFailed("Delete record in database failed with message:\n" + ex.Message, DeleteAssetFailureType.DeleteRecordFailed);
            }

            // Try delete file
            try
            {
                // Delete file from storage
                await _fileStorage.Delete(assetId);

            }
            catch (Exception ex)
            {
                // Delete file failed
                return DeleteFailed(
                    "Delete file failed with message:\n"
                    + ex.Message
                    + $"\nThere will be file left in the storage with name: {files[0].Id.ToString()}.",
                    DeleteAssetFailureType.DeleteFileFailed);
            }

            // Create resonse Dto
            var response = new DeleteAudioAssetResponse(
                assetId,
                asset.Title
                );

            // Return response
            return DeleteSuccess(assetId, response);
        }

        private DeleteAudioAssetResult DeleteSuccess(Guid audioAssetId, DeleteAudioAssetResponse response)
        {
            return new DeleteAudioAssetResult(true, null, null, audioAssetId, response);
        }
        private DeleteAudioAssetResult DeleteFailed(string errorMessage, DeleteAssetFailureType failureType)
        {
            return new DeleteAudioAssetResult(false, errorMessage, failureType, null, null);
        }

        public record DeleteAudioAssetResult(bool Success, string? ErrorMessage, DeleteAssetFailureType? FailureType, Guid? AudioAssetId, DeleteAudioAssetResponse? response);
    }

    public enum DeleteAssetFailureType
    {
        AssetNotFound = 0,
        FileNotFound = 1,
        DeleteFileFailed = 2,
        DeleteRecordFailed = 3,
    }
}
