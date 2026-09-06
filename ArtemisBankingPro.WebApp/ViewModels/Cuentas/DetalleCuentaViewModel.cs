using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Application.Dtos.Transactions;

namespace ArtemisBankingPro.WebApp.ViewModels.Cuentas;

/// <summary>Detalle de una cuenta de ahorro con sus transacciones paginadas.</summary>
public class DetalleCuentaViewModel
{
    public SavingsAccountDto Cuenta { get; set; } = null!;
    public PagedResult<TransactionDto> Transacciones { get; set; } = new();
}
