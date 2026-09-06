using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Cuentas;

/// <summary>Asignación de cuenta de ahorro secundaria: cliente + balance inicial.</summary>
public class AsignarCuentaViewModel
{
    public List<UserDto> Clientes { get; set; } = [];

    [Required(ErrorMessage = Mensajes.DebeSeleccionarCliente)]
    public string? ClientId { get; set; }

    [Required(ErrorMessage = "El balance inicial es obligatorio.")]
    [Range(0, 999999999999.99, ErrorMessage = Mensajes.BalanceInicialNegativo)]
    [Display(Name = "Balance inicial")]
    public decimal? InitialBalance { get; set; }
}
