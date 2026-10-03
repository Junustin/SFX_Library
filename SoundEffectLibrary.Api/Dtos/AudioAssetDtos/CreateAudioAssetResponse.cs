namespace SoundEffectLibrary.Api.Dtos
{
    public record CreateAudioAssetResponse(Guid Id, string Title, string Description, int CategoryId);
}
