using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Users;

namespace ArtemisBankingPro.WebApp.ViewModels.Usuarios;

public class UsuariosIndexViewModel
{
    public PagedResult<UserDto> Usuarios { get; set; } = new();

    /// <summary>Filtro por rol (Administrador/Cajero/Cliente); null = todos (Comercio excluido).</summary>
    public string? Rol { get; set; }

    /// <summary>Id del administrador autenticado (para bloquear acciones sobre sí mismo).</summary>
    public string UsuarioActualId { get; set; } = null!;
}
