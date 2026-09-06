using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.Core.Application.Helpers;

/// <summary>
/// Avance de efectivo: interés fijo del 6.25%. La cuenta recibe SOLO el avance;
/// la tarjeta carga avance + interés. Ejemplo del spec: avance 100 → interés 6.25,
/// la cuenta recibe 100 y la deuda sube 106.25.
/// </summary>
public static class CashAdvanceCalculator
{
    public static decimal Interest(decimal advanceAmount) =>
        Math.Round(advanceAmount * AppConstants.CashAdvanceInterestRate, 2, MidpointRounding.AwayFromZero);

    public static decimal TotalToCharge(decimal advanceAmount) =>
        advanceAmount + Interest(advanceAmount);

    public static decimal AvailableCredit(decimal creditLimit, decimal owedAmount) =>
        creditLimit - owedAmount;

    /// <summary>Aprobado solo si el total (avance + interés) cabe en el crédito disponible.</summary>
    public static bool IsApproved(decimal advanceAmount, decimal creditLimit, decimal owedAmount) =>
        TotalToCharge(advanceAmount) <= AvailableCredit(creditLimit, owedAmount);
}
