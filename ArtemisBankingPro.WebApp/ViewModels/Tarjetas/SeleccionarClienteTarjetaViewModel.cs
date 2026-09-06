using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Tarjetas;

/// <summary>Paso 1 de asignación de tarjeta: cualquier cliente activo.</summary>
public class SeleccionarClienteTarjetaViewModel
{
    public List<UserDto> Clientes { get; set; } = [];

    [Required(ErrorMessage = Mensajes.DebeSeleccionarCliente)]
    public string? ClientId { get; set; }
}
