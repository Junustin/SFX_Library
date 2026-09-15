namespace SoundEffectLibrary.Api.Api.Services
{
    public class AudioUploadOptions
    {
        public long MaxFileSize { get; set; }
        public string[] AllowedContentTypes { get; set; } = [];
    }

    public class AudioStorageOptions
    {
        public string RootPath { get; set; } = null!;
    }
}
