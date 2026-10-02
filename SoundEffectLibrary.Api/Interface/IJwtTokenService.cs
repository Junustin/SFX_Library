namespace SoundEffectLibrary.Api.Interface
{
    public interface IJwtTokenService
    {
        public string GetAssetManagerToken();
        public string GetNoRoleToken();
    }
}
