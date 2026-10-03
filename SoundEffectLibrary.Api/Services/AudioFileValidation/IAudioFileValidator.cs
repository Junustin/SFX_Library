namespace SoundEffectLibrary.Api.Interface
{
    public interface IAudioFileValidator
    {
        FileValidationResult Validate(IFormFile file);
    }

    public record FileValidationResult(bool IsValid, string? ErrorMessage);
}
