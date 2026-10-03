namespace SoundEffectLibrary.Api.Dtos
{
    public record GetAudioAssetResponse(List<AudioAssetCardResponse> Items, int Page, int PageSize, int TotalCount);
}
