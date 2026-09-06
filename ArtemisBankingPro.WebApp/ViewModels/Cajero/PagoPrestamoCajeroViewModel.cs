using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Cajero;

/// <summary>Pago a préstamo en ventanilla: número de cuenta y de préstamo digitados manualmente.</summary>
public class PagoPrestamoCajeroViewModel
{
    [Required(ErrorMessage = "El número de cuenta es obligatorio.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = Mensajes.CuentaInvalida)]
    [Display(Name = "Número de cuenta")]
    public string? AccountNumber { get; set; }

    [Required(ErrorMessage = "El número de préstamo es obligatorio.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = Mensajes.PrestamoInvalidoCajero)]
    [Display(Name = "Número de préstamo")]
    public string? LoanNumber { get; set; }

    [Required(ErrorMessage = Mensajes.MontoPagoInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.MontoPagoInvalido)]
    [Display(Name = "Monto a pagar")]
    public decimal? Amount { get; set; }
}
