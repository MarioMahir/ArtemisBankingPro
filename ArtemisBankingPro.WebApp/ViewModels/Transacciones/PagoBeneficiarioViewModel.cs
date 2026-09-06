using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Application.Dtos.Beneficiaries;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Transacciones;

public class PagoBeneficiarioViewModel
{
    public List<SavingsAccountDto> CuentasOrigen { get; set; } = [];
    public List<BeneficiaryDto> Beneficiarios { get; set; } = [];

    [Required(ErrorMessage = "Debe seleccionar un beneficiario.")]
    [Display(Name = "Beneficiario")]
    public int? BeneficiaryId { get; set; }

    [Required(ErrorMessage = "Debe seleccionar la cuenta de origen.")]
    [Display(Name = "Cuenta de origen")]
    public string? SourceAccountNumber { get; set; }

    [Required(ErrorMessage = Mensajes.MontoTransferenciaInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.MontoTransferenciaInvalido)]
    [Display(Name = "Monto")]
    public decimal? Amount { get; set; }
}
