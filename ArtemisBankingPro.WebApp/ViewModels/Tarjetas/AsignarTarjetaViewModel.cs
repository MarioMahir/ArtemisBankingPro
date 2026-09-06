using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Tarjetas;

/// <summary>Paso 2 de asignación de tarjeta: límite de crédito.</summary>
public class AsignarTarjetaViewModel
{
    [Required(ErrorMessage = Mensajes.DebeSeleccionarCliente)]
    public string ClientId { get; set; } = null!;

    public string? ClienteNombre { get; set; }
    public string? ClienteCedula { get; set; }

    [Required(ErrorMessage = Mensajes.LimiteInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.LimiteInvalido)]
    [Display(Name = "Límite de crédito")]
    public decimal? CreditLimit { get; set; }
}
