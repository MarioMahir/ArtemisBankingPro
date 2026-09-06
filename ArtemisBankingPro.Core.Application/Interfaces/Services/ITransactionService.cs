using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Transactions;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

/// <summary>
/// Operaciones de movimiento de dinero usadas por Cliente Y Cajero.
/// ownerUserId != null → operación de cliente (valida propiedad de los productos);
/// cashierId != null → operación de ventanilla (se asocia al cajero y usa sus mensajes).
/// Los métodos Prepare* validan y devuelven los datos de la pantalla de confirmación previa
/// sin mutar nada; los métodos de ejecución realizan la operación transaccional.
/// </summary>
public interface ITransactionService
{
    // ---- Transacción express / terceros ----
    Task<ServiceResult<TransactionConfirmationDto>> PrepareExpressTransferAsync(
        string sourceAccountNumber, string destinationAccountNumber, decimal amount,
        string? ownerUserId = null, string? cashierId = null);

    Task<ServiceResult> ExpressTransferAsync(
        string sourceAccountNumber, string destinationAccountNumber, decimal amount,
        string? ownerUserId = null, string? cashierId = null);

    // ---- Pago a tarjeta de crédito ----
    Task<ServiceResult<TransactionConfirmationDto>> PrepareCreditCardPaymentAsync(
        string sourceAccountNumber, string cardNumber, decimal amount,
        string? ownerUserId = null, string? cashierId = null);

    Task<ServiceResult> PayCreditCardAsync(
        string sourceAccountNumber, string cardNumber, decimal amount,
        string? ownerUserId = null, string? cashierId = null);

    // ---- Pago a préstamo ----
    Task<ServiceResult<TransactionConfirmationDto>> PrepareLoanPaymentAsync(
        string sourceAccountNumber, string loanNumber, decimal amount,
        string? ownerUserId = null, string? cashierId = null);

    Task<ServiceResult> PayLoanAsync(
        string sourceAccountNumber, string loanNumber, decimal amount,
        string? ownerUserId = null, string? cashierId = null);

    // ---- Transferencia entre cuentas propias ----
    Task<ServiceResult<TransactionConfirmationDto>> PrepareTransferBetweenOwnAccountsAsync(
        string ownerUserId, string sourceAccountNumber, string destinationAccountNumber, decimal amount);

    Task<ServiceResult> TransferBetweenOwnAccountsAsync(
        string ownerUserId, string sourceAccountNumber, string destinationAccountNumber, decimal amount);

    // ---- Transferencia a beneficiario ----
    Task<ServiceResult<TransactionConfirmationDto>> PrepareTransferToBeneficiaryAsync(
        string ownerUserId, int beneficiaryId, string sourceAccountNumber, decimal amount);

    Task<ServiceResult> TransferToBeneficiaryAsync(
        string ownerUserId, int beneficiaryId, string sourceAccountNumber, decimal amount);

    // ---- Cajero: depósito y retiro ----
    Task<ServiceResult<TransactionConfirmationDto>> PrepareDepositAsync(string accountNumber, decimal amount);
    Task<ServiceResult> DepositAsync(string accountNumber, decimal amount, string cashierId);

    Task<ServiceResult<TransactionConfirmationDto>> PrepareWithdrawAsync(string accountNumber, decimal amount);
    Task<ServiceResult> WithdrawAsync(string accountNumber, decimal amount, string cashierId);
}
