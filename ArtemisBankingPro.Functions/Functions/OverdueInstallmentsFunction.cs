using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArtemisBankingPro.Functions.Functions;

/// <summary>
/// Proceso automático DIARIO exigido por el spec: marca como atrasadas las cuotas
/// cuya fecha de vencimiento ya pasó y no han sido pagadas completamente.
/// Un préstamo con al menos una cuota atrasada se considera "en mora".
/// </summary>
public class OverdueInstallmentsFunction(AppDbContext context, ILogger<OverdueInstallmentsFunction> logger)
{
    [Function("MarkOverdueInstallments")]
    public async Task RunAsync([TimerTrigger("0 0 1 * * *", RunOnStartup = false)] TimerInfo timer)
    {
        var today = DateTime.Now.Date;

        // Atrasada = vencida y no pagada por completo (Pendiente o ParcialmentePagada).
        var overdue = await context.LoanInstallments
            .Where(i => i.DueDate < today
                && i.Status != InstallmentStatus.Pagada
                && !i.IsOverdue)
            .ToListAsync();

        foreach (var installment in overdue)
            installment.IsOverdue = true;

        // Consistencia inversa: si una cuota marcada fue saldada, se limpia el indicador.
        var cleared = await context.LoanInstallments
            .Where(i => i.IsOverdue && i.Status == InstallmentStatus.Pagada)
            .ToListAsync();

        foreach (var installment in cleared)
            installment.IsOverdue = false;

        await context.SaveChangesAsync();

        logger.LogInformation(
            "Proceso de cuotas atrasadas completado: {Marked} marcadas, {Cleared} limpiadas.",
            overdue.Count, cleared.Count);
    }
}
