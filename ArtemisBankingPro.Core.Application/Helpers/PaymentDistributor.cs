using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Application.Helpers;

/// <summary>
/// Aplicación de pagos a la tabla de amortización: siempre a la cuota pendiente
/// más antigua primero, marcando Pagada/ParcialmentePagada y quitando el atraso
/// cuando la cuota se salda por completo.
/// </summary>
public static class PaymentDistributor
{
    /// <summary>Regla anti-sobrepago: nunca se debita más que la deuda real.</summary>
    public static decimal EffectiveAmount(decimal requestedAmount, decimal actualDebt) =>
        Math.Min(requestedAmount, actualDebt);

    /// <summary>
    /// Distribuye el monto efectivo sobre las cuotas (ordenadas por vencimiento).
    /// Muta las cuotas recibidas y devuelve true si TODAS quedaron pagadas
    /// (el préstamo debe pasar a Completado).
    /// </summary>
    public static bool ApplyPayment(IEnumerable<LoanInstallment> installments, decimal effectiveAmount)
    {
        var ordered = installments.OrderBy(i => i.DueDate).ThenBy(i => i.Number).ToList();
        var remaining = effectiveAmount;

        foreach (var installment in ordered.Where(i => i.Status != InstallmentStatus.Pagada))
        {
            if (remaining <= 0) break;

            var applied = Math.Min(remaining, installment.PendingAmount);
            installment.PendingAmount -= applied;
            remaining -= applied;

            if (installment.PendingAmount == 0)
            {
                installment.Status = InstallmentStatus.Pagada;
                installment.IsOverdue = false;
            }
            else
            {
                installment.Status = InstallmentStatus.ParcialmentePagada;
            }
        }

        return ordered.All(i => i.Status == InstallmentStatus.Pagada);
    }
}
