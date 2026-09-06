using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Infrastructure.Identity.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ArtemisBankingPro.Infrastructure.Identity.Services;

public class JwtService(IOptions<JwtSettings> settings) : IJwtService
{
    private readonly JwtSettings _settings = settings.Value;

    public string GenerateToken(UserDto user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        // Hermes Pay: el rol Comercio opera SIEMPRE sobre su propio comercio,
        // tomado de este claim (la API ignora el commerceId de la URL).
        if (user.CommerceId is not null)
            claims.Add(new Claim("commerceId", user.CommerceId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_settings.DurationInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
