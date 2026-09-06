using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;

namespace ArtemisBankingPro.WebApp.ViewModels.Cliente;

/// <summary>Home del cliente: cuentas (principal primero), préstamos y tarjetas activos.</summary>
public class ClienteHomeViewModel
{
    public List<SavingsAccountDto> Cuentas { get; set; } = [];
    public List<LoanDto> Prestamos { get; set; } = [];
    public List<CreditCardDto> Tarjetas { get; set; } = [];

    public bool SinProductos => Cuentas.Count == 0 && Prestamos.Count == 0 && Tarjetas.Count == 0;
}
