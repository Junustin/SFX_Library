using SoundEffectLibrary.Models;

namespace SoundEffectLibrary.Repository
{
    public class InMemoryAudioAssetRepository
    {
        private List<AudioAsset> _audioAssets = [];

        public void Add(AudioAsset audioAsset)
        {
            _audioAssets.Add(audioAsset);
        }

        public IReadOnlyList<AudioAsset> GetAll()
        {
            IReadOnlyList<AudioAsset> list = _audioAssets;
            return list;
        }
    }
}
