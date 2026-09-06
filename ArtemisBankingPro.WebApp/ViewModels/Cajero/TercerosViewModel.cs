using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Cajero;

/// <summary>Transacción a cuentas de terceros en ventanilla.</summary>
public class TercerosViewModel
{
    [Required(ErrorMessage = "El número de cuenta origen es obligatorio.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = Mensajes.CuentaOrigenInvalida)]
    [Display(Name = "Cuenta origen")]
    public string? SourceAccountNumber { get; set; }

    [Required(ErrorMessage = "El número de cuenta destino es obligatorio.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = Mensajes.CuentaDestinoInvalida)]
    [Display(Name = "Cuenta destino")]
    public string? DestinationAccountNumber { get; set; }

    [Required(ErrorMessage = Mensajes.MontoTransferenciaInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.MontoTransferenciaInvalido)]
    [Display(Name = "Monto")]
    public decimal? Amount { get; set; }
}
