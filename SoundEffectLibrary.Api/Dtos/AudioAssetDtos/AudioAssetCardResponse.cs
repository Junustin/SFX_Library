namespace SoundEffectLibrary.Api.Dtos
{
    public record AudioAssetCardResponse(Guid Id, string Title, CategoryResponse CategoryResponse, string PreviewFileUrl);
}
