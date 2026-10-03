using System.ComponentModel.DataAnnotations;

namespace SoundEffectLibrary.Api.Dtos
{
    public record GetAudioAssetRequest(string? Search, [Range(1, int.MaxValue)] int Page, [Range(1, 100)] int PageSize);
}
