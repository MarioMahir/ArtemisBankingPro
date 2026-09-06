using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Transacciones;

/// <summary>Pago a tarjeta de crédito propia (el cliente selecciona por últimos 4, nunca digita el número).</summary>
public class PagoTarjetaViewModel
{
    public List<SavingsAccountDto> CuentasOrigen { get; set; } = [];
    public List<CreditCardDto> Tarjetas { get; set; } = [];

    [Required(ErrorMessage = "Debe seleccionar la cuenta de origen.")]
    [Display(Name = "Cuenta de origen")]
    public string? SourceAccountNumber { get; set; }

    [Required(ErrorMessage = "Debe seleccionar la tarjeta a pagar.")]
    [Display(Name = "Tarjeta de crédito")]
    public int? CardId { get; set; }

    [Required(ErrorMessage = Mensajes.MontoPagoInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.MontoPagoInvalido)]
    [Display(Name = "Monto a pagar")]
    public decimal? Amount { get; set; }
}
