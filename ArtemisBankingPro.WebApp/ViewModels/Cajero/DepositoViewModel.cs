using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Cajero;

public class DepositoViewModel
{
    [Required(ErrorMessage = "El número de cuenta es obligatorio.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = Mensajes.CuentaInvalida)]
    [Display(Name = "Número de cuenta")]
    public string? AccountNumber { get; set; }

    [Required(ErrorMessage = Mensajes.MontoDepositoInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.MontoDepositoInvalido)]
    [Display(Name = "Monto a depositar")]
    public decimal? Amount { get; set; }
}
