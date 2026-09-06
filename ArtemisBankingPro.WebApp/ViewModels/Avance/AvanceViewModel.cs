using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Avance;

public class AvanceViewModel
{
    public List<CreditCardDto> Tarjetas { get; set; } = [];
    public List<SavingsAccountDto> Cuentas { get; set; } = [];

    [Required(ErrorMessage = "Debe seleccionar la tarjeta de origen.")]
    [Display(Name = "Tarjeta de crédito")]
    public int? CardId { get; set; }

    [Required(ErrorMessage = "Debe seleccionar la cuenta de destino.")]
    [Display(Name = "Cuenta de destino")]
    public string? DestinationAccountNumber { get; set; }

    [Required(ErrorMessage = Mensajes.MontoAvanceInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.MontoAvanceInvalido)]
    [Display(Name = "Monto del avance")]
    public decimal? Amount { get; set; }
}
