using System.Security.Claims;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApp.Controllers;

public class HomeController(ILogger<HomeController> logger) : Controller
{
    /// <summary>Redirige al Home del rol autenticado; sin autenticar → Login.</summary>
    [HttpGet]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction("Login", "Account");

        return User.FindFirstValue(ClaimTypes.Role) switch
        {
            Roles.Administrador => RedirectToAction("Index", "Admin"),
            Roles.Cajero => RedirectToAction("Index", "Cajero"),
            Roles.Cliente => RedirectToAction("Index", "Cliente"),
            _ => RedirectToAction("Login", "Account")
        };
    }

    /// <summary>Página de error amigable del global exception handler (estilo Problem Details).</summary>
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (feature?.Error is not null)
        {
            logger.LogError(feature.Error, "Error no controlado en {Ruta}", feature.Path);
        }

        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View(new ErrorViewModel
        {
            Status = StatusCodes.Status500InternalServerError,
            RequestId = HttpContext.TraceIdentifier
        });
    }
}
