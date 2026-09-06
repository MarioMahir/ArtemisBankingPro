using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Transacciones;

public class PagoPrestamoViewModel
{
    public List<SavingsAccountDto> CuentasOrigen { get; set; } = [];
    public List<LoanDto> Prestamos { get; set; } = [];

    [Required(ErrorMessage = "Debe seleccionar la cuenta de origen.")]
    [Display(Name = "Cuenta de origen")]
    public string? SourceAccountNumber { get; set; }

    [Required(ErrorMessage = "Debe seleccionar el préstamo a pagar.")]
    [Display(Name = "Préstamo")]
    public string? LoanNumber { get; set; }

    [Required(ErrorMessage = Mensajes.MontoPagoInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.MontoPagoInvalido)]
    [Display(Name = "Monto a pagar")]
    public decimal? Amount { get; set; }
}
