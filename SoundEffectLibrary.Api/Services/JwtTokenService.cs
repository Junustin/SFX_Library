using Microsoft.IdentityModel.Tokens;
using SoundEffectLibrary.Api.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SoundEffectLibrary.Api.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private IConfiguration _configuration;
        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string GetAssetManagerToken()
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SigningKey"]!));

            var credential = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "dev-user"));

            claims.Add(new Claim(ClaimTypes.Role, "AssetManager"));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: credential
                );

            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(token);
        }

        public string GetNoRoleToken()
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SigningKey"]!));

            var credential = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "dev-user"));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: credential
                );

            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(token);
        }
    }
}
