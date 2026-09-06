using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Transacciones;

public class ExpressViewModel
{
    public List<SavingsAccountDto> CuentasOrigen { get; set; } = [];

    [Required(ErrorMessage = "Debe seleccionar la cuenta de origen.")]
    [Display(Name = "Cuenta de origen")]
    public string? SourceAccountNumber { get; set; }

    [Required(ErrorMessage = "El número de cuenta destino es obligatorio.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = Mensajes.CuentaInvalida)]
    [Display(Name = "Cuenta destino")]
    public string? DestinationAccountNumber { get; set; }

    [Required(ErrorMessage = Mensajes.MontoTransferenciaInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.MontoTransferenciaInvalido)]
    [Display(Name = "Monto")]
    public decimal? Amount { get; set; }
}
