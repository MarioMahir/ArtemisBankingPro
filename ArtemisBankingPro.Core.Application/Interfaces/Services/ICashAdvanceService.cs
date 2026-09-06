using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.CashAdvances;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

public interface ICashAdvanceService
{
    /// <summary>Valida y devuelve avance/interés/total para la pantalla de confirmación.</summary>
    Task<ServiceResult<CashAdvanceConfirmationDto>> PrepareAsync(
        string ownerUserId, int cardId, string destinationAccountNumber, decimal advanceAmount);

    /// <summary>
    /// Ejecuta el avance: la tarjeta carga avance + interés (consumo "AVANCE" por el total),
    /// la cuenta recibe SOLO el avance. Rechazo → consumo RECHAZADO sin tocar nada.
    /// </summary>
    Task<ServiceResult> ExecuteAsync(
        string ownerUserId, int cardId, string destinationAccountNumber, decimal advanceAmount);
}
