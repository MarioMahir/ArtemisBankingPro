using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Cajero;

/// <summary>Pago a tarjeta en ventanilla: número de cuenta y de tarjeta digitados manualmente.</summary>
public class PagoTarjetaCajeroViewModel
{
    [Required(ErrorMessage = "El número de cuenta es obligatorio.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = Mensajes.CuentaInvalida)]
    [Display(Name = "Número de cuenta")]
    public string? AccountNumber { get; set; }

    [Required(ErrorMessage = "El número de tarjeta es obligatorio.")]
    [RegularExpression(@"^\d{16}$", ErrorMessage = Mensajes.TarjetaInvalidaCajero)]
    [Display(Name = "Número de tarjeta")]
    public string? CardNumber { get; set; }

    [Required(ErrorMessage = Mensajes.MontoPagoInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.MontoPagoInvalido)]
    [Display(Name = "Monto a pagar")]
    public decimal? Amount { get; set; }
}
