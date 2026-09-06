using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Transferencia;

/// <summary>Transferencia entre cuentas propias (requiere al menos dos activas).</summary>
public class TransferenciaViewModel
{
    public List<SavingsAccountDto> Cuentas { get; set; } = [];

    [Required(ErrorMessage = "Debe seleccionar la cuenta de origen.")]
    [Display(Name = "Cuenta de origen")]
    public string? SourceAccountNumber { get; set; }

    [Required(ErrorMessage = "Debe seleccionar la cuenta de destino.")]
    [Display(Name = "Cuenta de destino")]
    public string? DestinationAccountNumber { get; set; }

    [Required(ErrorMessage = Mensajes.MontoTransferenciaInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.MontoTransferenciaInvalido)]
    [Display(Name = "Monto")]
    public decimal? Amount { get; set; }
}
