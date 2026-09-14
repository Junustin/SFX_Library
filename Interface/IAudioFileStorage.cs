namespace SoundEffectLibrary.Interface
{
    public interface IAudioFileStorage
    {
        Task<string> SaveAsync(Stream stream, Guid AssetId,string ContentType, CancellationToken cancellationToken = default);

        Task<Stream> GetAsync(string storageKey, CancellationToken cancellationToken = default);

        Task<bool> Delete(Guid assetId);
        
    }
}
