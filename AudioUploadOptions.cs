namespace SoundEffectLibrary
{
    public class AudioUploadOptions
    {
        public long MaxFileSize { get; set; }
        public string[] AllowedContentTypes { get; set; } = [];
    }
}
