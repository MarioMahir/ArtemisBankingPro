using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Cajero;

public class RetiroViewModel
{
    [Required(ErrorMessage = "El número de cuenta es obligatorio.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = Mensajes.CuentaInvalida)]
    [Display(Name = "Número de cuenta")]
    public string? AccountNumber { get; set; }

    [Required(ErrorMessage = Mensajes.MontoRetiroInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.MontoRetiroInvalido)]
    [Display(Name = "Monto a retirar")]
    public decimal? Amount { get; set; }
}
