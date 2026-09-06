using ArtemisBankingPro.Core.Application.Dtos.Dashboard;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

public interface IDashboardService
{
    /// <summary>Los 11 indicadores del administrador.</summary>
    Task<DashboardDto> GetAdminDashboardAsync();

    /// <summary>Los 4 indicadores del cajero: solo sus operaciones de HOY.</summary>
    Task<CashierDashboardDto> GetCashierDashboardAsync(string cashierId);
}
