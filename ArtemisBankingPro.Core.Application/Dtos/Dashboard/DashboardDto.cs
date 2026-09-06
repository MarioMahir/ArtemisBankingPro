namespace ArtemisBankingPro.Core.Application.Dtos.Dashboard;

/// <summary>Los 11 indicadores del dashboard del administrador (reglas §4 de docs/reglas-negocio.md).</summary>
public class DashboardDto
{
    public int TotalTransactions { get; set; }
    public int TransactionsToday { get; set; }
    public int TotalPayments { get; set; }
    public int PaymentsToday { get; set; }
    public int ActiveClients { get; set; }
    public int InactiveClients { get; set; }
    public int TotalActiveProducts { get; set; }
    public int ActiveLoans { get; set; }
    public int ActiveCreditCards { get; set; }
    public int ActiveSavingsAccounts { get; set; }
    public decimal AverageDebtPerClient { get; set; }
}
