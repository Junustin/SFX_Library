namespace SoundEffectLibrary.Api.Configuration
{
    public class AudioUploadOptions
    {
        public long MaxFileSize { get; set; }
        public string[] AllowedContentTypes { get; set; } = [];
    }
}