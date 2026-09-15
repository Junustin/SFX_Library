using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using SoundEffectLibrary.Interface;

namespace SoundEffectLibrary.Services
{
    public class AudioFileValidator : IAudioFileValidator
    {
        private readonly AudioUploadOptions _options;

        public AudioFileValidator(IOptions<AudioUploadOptions> options)
        {
            _options = options.Value;
        }

        public FileValidationResult Validate(IFormFile file)
        {
            if (file.Length <= 0)
                return FailValidation("No file/Empty file");
            else if (file.Length > _options.MaxFileSize)
                return FailValidation("File too large");

            var contentType = file.ContentType;
            if (string.IsNullOrEmpty(contentType) || !_options.AllowedContentTypes.Contains(contentType))
                return FailValidation("Invalid content type");

            var provider = new FileExtensionContentTypeProvider();
            if (provider.TryGetContentType(file.FileName, out var expectedMimeType))
            {
                if (!string.Equals(file.ContentType, expectedMimeType, StringComparison.OrdinalIgnoreCase))
                {
                    // Type mismatch might be danger file
                    return FailValidation("Type mismatch");
                }
            }
            else
            {
                // Unknow Mime type
                return FailValidation("Unknown file type");
            }

            // Validation success
            return SuccessValidation();
        }

        private FileValidationResult FailValidation(string errorMessage)
        {
            return new FileValidationResult(false, errorMessage);
        }

        private FileValidationResult SuccessValidation()
        {
            return new FileValidationResult(true, null);
        }
    } 
}
