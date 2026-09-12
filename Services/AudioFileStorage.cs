using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using SoundEffectLibrary.Interface;

namespace SoundEffectLibrary.Services
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
            string storageKey = Path.Combine(AssetId.ToString(), guid.ToString()+extension);

            // Combine root with storage key for file creation
            string filePath = Path.Combine(_root.RootPath, storageKey);

            // Create physical file
            var createdFile = File.Create(filePath);

            // Copy bytes from input stream
            using (createdFile)
            {
                await stream.CopyToAsync(createdFile, cancellationToken);
            }
            
            // Return storage key
            return storageKey;
        }
        public Task<Stream> GetAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
