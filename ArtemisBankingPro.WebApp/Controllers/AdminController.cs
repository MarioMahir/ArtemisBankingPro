using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApp.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class AdminController(IDashboardService dashboardService) : Controller
{
    /// <summary>Home del administrador: los 11 indicadores del sistema.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var dashboard = await dashboardService.GetAdminDashboardAsync();
        return View(dashboard);
    }
}
