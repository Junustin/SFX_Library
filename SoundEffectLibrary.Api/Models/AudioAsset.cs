using System.ComponentModel.DataAnnotations;

namespace SoundEffectLibrary.Api.Models
{
    public class AudioAsset{
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;
        public ICollection<AudioFile> AudioFiles { get; set; } = new List<AudioFile>();
    }
    public record CreateAudioAssetRequest(string Title, string Description, int CategoryId, IFormFile File);
    public record CreateAudioAssetResponse(Guid Id,string Title, string Description, int CategoryId);
    public record AudioAssetCardResponse(Guid Id, string Title, CategoryResponse CategoryResponse, string PreviewFileUrl);
    public record GetAudioAssetRequest(string? Search, [Range(1, int.MaxValue)] int Page ,[Range(1, 100)] int PageSize);
    public record GetAudioAssetRespose(List<AudioAssetCardResponse> Items, int Page, int PageSize, int TotalCount);
    
}
