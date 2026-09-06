using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Application.Dtos.Transactions;
using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

public interface ISavingsAccountService
{
    /// <summary>
    /// Listado paginado (20). Sin filtro de estado: activas primero, luego canceladas,
    /// cada grupo más reciente primero. La cédula se resuelve contra Identity.
    /// </summary>
    Task<ServiceResult<PagedResult<SavingsAccountDto>>> GetPagedAsync(
        int page, ProductStatus? status, AccountType? type, string? identification);

    /// <summary>Asigna una cuenta SECUNDARIA a un cliente activo con principal activa.</summary>
    Task<ServiceResult<SavingsAccountDto>> CreateSecondaryAsync(string clientId, decimal? initialBalance, string adminUserId);

    /// <summary>Cancela una secundaria transfiriendo el balance a la principal (transaccional).</summary>
    Task<ServiceResult> CancelAsync(string accountNumber);

    /// <summary>Transacciones de la cuenta, paginadas, más recientes primero.</summary>
    Task<ServiceResult<PagedResult<TransactionDto>>> GetTransactionsAsync(string accountNumber, int page);

    /// <summary>Cuentas activas del usuario: Principal primero, secundarias por balance descendente.</summary>
    Task<List<SavingsAccountDto>> GetActiveAccountsByUserAsync(string userId);

    /// <summary>Cuenta (activa o cancelada) por número, con los datos del titular; null si no existe.</summary>
    Task<SavingsAccountDto?> GetByNumberAsync(string accountNumber);
}
