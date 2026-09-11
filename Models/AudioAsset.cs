namespace SoundEffectLibrary.Models
{
    public class AudioAsset {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int CategoryId { get; set; }
    }
    public record CreateAudioAssetRequest(string Title, string Description, int CategoryId);
}
