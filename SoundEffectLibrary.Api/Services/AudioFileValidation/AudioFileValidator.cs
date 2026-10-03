using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using SoundEffectLibrary.Api.Configuration;
using SoundEffectLibrary.Api.Interface;

namespace SoundEffectLibrary.Api.Services
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
                return FailValidation("Unsupported content type.");

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

            // Check byte signature for validate actual audio file
            using Stream stream = file.OpenReadStream();
            switch (contentType)
            {
                case "audio/wav":
                    if (HasWavSignature(stream))
                        return SuccessValidation();
                    break;

                case "audio/mpeg":
                    if (IsMp3(stream))
                        return SuccessValidation();
                    break;

                default:
                    return FailValidation("Unsupported content type.");
            }
           
            // Validation failed
            return FailValidation("File does not match the declared content type.");
        }

        private bool IsMp3(Stream stream)
        {
            if (!stream.CanRead)
                throw new InvalidOperationException("Stream cannot be read.");

            if (!stream.CanSeek)
                throw new InvalidOperationException("Stream must support seeking.");

            stream.Position = 0;

            if (HasId3v2Tag(stream))
            {
                stream.Position = 0;

                byte[] id3Header = GetFileHeader(stream, 10);

                if (id3Header.Length < 10)
                    return false;

                if (!IsValidId3v2TagSize(id3Header))
                    return false;

                int tagSize = GetId3v2TagSize(id3Header);

                try
                {
                    SkipBytes(stream, tagSize);
                }
                catch (EndOfStreamException)
                {
                    return false;
                }
            }
            else
            {
                stream.Position = 0;
            }

            byte[] header = GetFileHeader(stream, 2);

            if (header.Length < 2)
                return false;

            if (header[0] != 0xFF)
                return false;

            if ((header[1] & 0b11100000) != 0b11100000)
                return false;

            byte version = (byte)((header[1] & 0b00011000) >> 3);

            if (version == 1)
                return false;

            byte layer = (byte)((header[1] & 0b00000110) >> 1);

            if (layer == 0)
                return false;

            return true;
        }

        private bool HasId3v2Tag(Stream stream)
        {
            stream.Position = 0;

            byte[] header = GetFileHeader(stream, 3);

            if (header.Length < 3)
                return false;

            if(header[0] == 0x49 &&
               header[1] == 0x44 &&
               header[2] == 0x33
               )
            {
                return true;
            }

            return false;
        }

        private int GetId3v2TagSize(byte[] header)
        {
            if (header.Length < 10)
                throw new ArgumentException("ID3v2 header must be at least 10 bytes.");

            int size = (header[6] << 21) 
                | (header[7] << 14) 
                | (header[8] << 7) 
                | header[9];

            return size;
        }

        private bool IsValidId3v2TagSize(byte[] header)
        {
            if (header.Length < 10)
                return false;

            for (int i = 6; i <= 9; i++)
            {
                if ((header[i] & 0b10000000) != 0)
                    return false;
            }

            return true;
        }

        private bool HasWavSignature(Stream stream)
        {
            byte[] header = GetFileHeader(stream, 12);

            if (header.Length < 12)
                return false;

            if (
                header[0] == 0x52 && 
                header[1] == 0x49 &&
                header[2] == 0x46 &&
                header[3] == 0x46 &&
                header[8] == 0x57 &&
                header[9] == 0x41 &&
                header[10] == 0x56 &&
                header[11] == 0x45 
                )
            {
                return true;
            }

            return false;
        }

        private void SkipBytes(Stream stream, int count)
        {
            if (!stream.CanRead)
                throw new InvalidOperationException("Stream cannot be read.");

            byte[] buffer = new byte[4096];

            while (count > 0)
            {
                int bytesToRead = Math.Min(count, buffer.Length);

                int bytesRead = stream.Read(
                    buffer,
                    0,
                    bytesToRead);

                if (bytesRead == 0)
                    throw new EndOfStreamException();

                count -= bytesRead;
            }
        }

        private byte[] GetFileHeader(Stream stream, int headerSize = 4)
        {
            if (!stream.CanRead) 
                throw new InvalidOperationException("Stream cannot be read.");

            byte[] header = new byte[headerSize];

            try
            {
                stream.ReadExactly(header, 0, header.Length);
                return header;
            }
            catch (EndOfStreamException)
            {
                return Array.Empty<byte>();
            }
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
