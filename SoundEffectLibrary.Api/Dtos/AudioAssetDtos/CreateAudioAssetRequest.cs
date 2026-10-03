namespace SoundEffectLibrary.Api.Dtos
{
    public record CreateAudioAssetRequest(string Title, string Description, int CategoryId, IFormFile File);
}
