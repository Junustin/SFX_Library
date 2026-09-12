using Microsoft.EntityFrameworkCore;
using SoundEffectLibrary.Data;
using SoundEffectLibrary.Interface;
using SoundEffectLibrary.Models;

namespace SoundEffectLibrary.Services
{
    public class CreateAudioAssetService
    {
        private readonly IAudioFileValidator _fileValidator;
        private readonly IAudioFileStorage _fileStorage;
        private readonly SfxDbContext _dbContext;
        public CreateAudioAssetService(
            IAudioFileValidator fileValidator,
            IAudioFileStorage fileStorage,
            SfxDbContext dbContext) 
        {
            _fileValidator = fileValidator;
            _fileStorage = fileStorage;
            _dbContext = dbContext;
        }

        public async Task<CreateAudioAssetResult> CreateAudioAssetAsync(CreateAudioAssetRequest request, CancellationToken cancellationToken)
        {
            // Validate file
            var validateResult = _fileValidator.Validate(request.File);
            if (!validateResult.IsValid)
            {
                // Validation failed
                return CreateFailed(validateResult.ErrorMessage!);
            }

            // Crate a asset record
            var audioAsset = new AudioAsset
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                CategoryId = request.CategoryId
            };

            // Save file to storage
            var storageKey = await _fileStorage.SaveAsync(request.File.OpenReadStream(), audioAsset.Id, request.File.ContentType, cancellationToken);

            // Create file record
            var audioFile = new AudioFile
            {
                Id = Guid.NewGuid(),
                AssetId = audioAsset.Id,
                FileName = request.File.FileName,
                ContentType = request.File.ContentType,
                FileSize = request.File.Length,
                StorageKey = storageKey
            };

            // Save to Database
            _dbContext.AudioAssets.Add(audioAsset);
            _dbContext.AudioFiles.Add(audioFile);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Create resonse Dto
            var responseDto = new CreateAudioAssetResponse
            (
                audioAsset.Id,
                audioAsset.Title,
                audioAsset.Description,
                audioAsset.CategoryId
            );

            return CreateSuccess(audioAsset.Id, responseDto);
        }

        private CreateAudioAssetResult CreateSuccess(Guid audioAssetId, CreateAudioAssetResponse response)
        {
            return new CreateAudioAssetResult(true, null, audioAssetId, response);
        }
        private CreateAudioAssetResult CreateFailed(string errorMessage)
        {
            return new CreateAudioAssetResult(false, errorMessage, null, null);
        }
    }

    public record CreateAudioAssetResult(bool Success, string? ErrorMessage, Guid? AudioAssetId,CreateAudioAssetResponse? response);
}
