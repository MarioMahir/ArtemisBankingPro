using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Users;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

/// <summary>
/// Orquestación de negocio sobre usuarios: creación con cuenta principal automática
/// (Cliente/Comercio), montos adicionales en edición y bloqueo de auto-edición.
/// </summary>
public interface IUserService
{
    /// <summary>Crea el usuario y, si es Cliente o Comercio, su cuenta de ahorro principal automática.</summary>
    Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequest request);

    /// <summary>Edita datos; AdditionalAmount &gt; 0 se suma a la cuenta principal con transacción CRÉDITO.</summary>
    Task<ServiceResult> UpdateUserAsync(UpdateUserRequest request, string actingUserId);

    /// <summary>Activa/inactiva. El administrador no puede modificar su propio estado.</summary>
    Task<ServiceResult> SetUserStatusAsync(string userId, bool active, string actingUserId);

    Task<PagedResult<UserDto>> GetPagedAsync(int page, string? role, bool onlyCommerce = false);
    Task<UserDto?> GetByIdAsync(string id);
}
