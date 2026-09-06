using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Users;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

/// <summary>
/// Operaciones sobre usuarios/roles/tokens de Identity. La orquestación de negocio
/// (cuenta principal automática, montos adicionales) vive en UserService (Application).
/// </summary>
public interface IAccountService
{
    // ---- Autenticación ----
    /// <summary>Valida credenciales, estado activo y rol permitido, con los mensajes exactos del spec.</summary>
    Task<ServiceResult<UserDto>> AuthenticateAsync(string userName, string password, string[] allowedRoles);

    // ---- Activación y reset ----
    Task<ServiceResult> ConfirmAccountAsync(string token);
    Task<ServiceResult> RequestPasswordResetAsync(string userName, bool tokenInBody, string[] allowedRoles);
    Task<ServiceResult> ResetPasswordAsync(string userId, string token, string password, string confirmPassword);

    // ---- CRUD de usuarios ----
    /// <summary>Crea el usuario INACTIVO, con rol, y envía el correo de activación.</summary>
    Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequest request);

    /// <summary>Actualiza datos (sin cambiar rol). Contraseña opcional.</summary>
    Task<ServiceResult> UpdateUserAsync(UpdateUserRequest request);

    /// <summary>Activa/inactiva. El administrador no puede modificar su propio estado.</summary>
    Task<ServiceResult> SetUserStatusAsync(string userId, bool active, string actingUserId);

    // ---- Consultas ----
    Task<UserDto?> GetByIdAsync(string id);
    Task<UserDto?> GetByUserNameAsync(string userName);
    Task<UserDto?> GetByIdentificationAsync(string identification);
    Task<UserDto?> GetByCommerceIdAsync(int commerceId);
    Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize, string? role, bool onlyCommerce);
    Task<List<UserDto>> GetActiveClientsAsync();
    Task<Dictionary<string, UserDto>> GetByIdsAsync(IEnumerable<string> ids);
    Task<int> CountByRoleAndStatusAsync(string role, bool active);

    /// <summary>Al desactivar un comercio, sus usuarios quedan inactivos (reactivar NO los reactiva).</summary>
    Task DeactivateUsersByCommerceAsync(int commerceId);

    /// <summary>Asocia un usuario (rol Comercio) a un comercio. Usado por el seeding y la API.</summary>
    Task<ServiceResult> AssignCommerceAsync(string userId, int commerceId);
}
