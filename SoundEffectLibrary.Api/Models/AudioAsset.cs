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
}