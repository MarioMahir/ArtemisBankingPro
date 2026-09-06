using ArtemisBankingPro.Core.Application.Dtos.Users;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

public interface IJwtService
{
    /// <summary>JWT con id de usuario, nombre de usuario, rol, fecha de emisión y expiración.</summary>
    string GenerateToken(UserDto user);
}
