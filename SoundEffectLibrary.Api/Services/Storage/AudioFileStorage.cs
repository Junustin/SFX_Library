using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using SoundEffectLibrary.Api.Configuration;
using SoundEffectLibrary.Api.Interface;

namespace SoundEffectLibrary.Api.Services
{
    public class AudioFileStorage : IAudioFileStorage
    {
        private readonly AudioStorageOptions _root;

        public AudioFileStorage(IOptions<AudioStorageOptions> root)
        {
            _root = root.Value;
        }
        public async Task<string> SaveAsync(Stream stream, Guid AssetId , string ContentType, CancellationToken cancellationToken = default)
        {
            // Create new GUID for this file
            Guid guid = Guid.NewGuid();

            // Detemine file extension
            var provider = new FileExtensionContentTypeProvider();
            var extension = provider.Mappings
                                .FirstOrDefault(m => m.Value
                                .Equals(ContentType, StringComparison.OrdinalIgnoreCase))
                                .Key;

            // Create directory for this AudioAsset
            Directory.CreateDirectory(Path.Combine(_root.RootPath, AssetId.ToString()));

            // Build storage key
            string storageKey = $"{AssetId}/{guid}{extension}";

            // Combine root with storage key for file creation
            string filePath = Path.Combine(_root.RootPath, storageKey);

            // Create physical file
            var createdFile = File.Create(filePath);

            try
            {
                // Copy bytes from input stream
                using (createdFile)
                {
                    await stream.CopyToAsync(createdFile, cancellationToken);
                }
            }
            catch
            {
                // If copy file failed delete that partial file
                if (File.Exists(filePath))
                {
                    File.Delete(filePath); 
                }
                throw;
            }
            
            // Return storage key
            return storageKey;
        }
        public async Task<Stream?> GetAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            // Combine root with storage key for file creation
            string filePath = Path.Combine(_root.RootPath, storageKey);

            if (!File.Exists(filePath))
                return null; // (STORAGE PROLEM meaning database has record of this file but there are no actual file)

            return File.OpenRead(filePath);
        }

        public bool IsAudioFileExist(string storageKey)
        {
            //Combine root with storage key for file creation
            string filePath = Path.Combine(_root.RootPath, storageKey);

            if (!File.Exists(filePath))
                    return false;

            return true;
        }

        public async Task<bool> Delete(Guid assetId)
        {
            string path = Path.Combine(_root.RootPath, assetId.ToString());
            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
