using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

/// <summary>Procesador de pagos Hermes Pay (API).</summary>
public interface IHermesPayService
{
    /// <summary>Consumos APROBADOS/RECHAZADOS del comercio, paginados, más recientes primero.</summary>
    Task<ServiceResult<PagedResult<ConsumptionDto>>> GetTransactionsAsync(int commerceId, int page);

    /// <summary>
    /// Procesa un pago con tarjeta contra el comercio: valida tarjeta (existencia, estado,
    /// vigencia MM-AAAA, CVC), comercio (activo, con usuario y cuenta principal activa) y
    /// crédito disponible. Aprobado → transaccional; rechazo por crédito → consumo RECHAZADO.
    /// </summary>
    Task<ServiceResult> ProcessPaymentAsync(
        int commerceId, string cardNumber, string monthExpirationCard, string yearExpirationCard,
        string cvc, decimal transactionAmount);
}
