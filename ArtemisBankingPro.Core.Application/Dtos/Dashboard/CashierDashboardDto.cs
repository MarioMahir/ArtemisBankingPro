namespace ArtemisBankingPro.Core.Application.Dtos.Dashboard;

/// <summary>Los 4 indicadores del cajero: SOLO sus operaciones y SOLO de hoy.</summary>
public class CashierDashboardDto
{
    public int TransactionsToday { get; set; }
    public int PaymentsToday { get; set; }
    public int DepositsToday { get; set; }
    public int WithdrawalsToday { get; set; }
}
