namespace SoundEffectLibrary.Models
{
    public class AudioFile
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }
        public string StorageKey { get; set; } = null!;

        // Navigation property
        public AudioAsset AudioAsset { get; set; } = null!;
    }
}
