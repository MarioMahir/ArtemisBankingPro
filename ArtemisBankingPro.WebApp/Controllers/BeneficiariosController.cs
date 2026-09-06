using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApp.ViewModels.Beneficiarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApp.Controllers;

[Authorize(Roles = Roles.Cliente)]
public class BeneficiariosController(
    IBeneficiaryService beneficiaryService,
    ILogger<BeneficiariosController> logger) : Controller
{
    private string UsuarioActualId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new BeneficiariosIndexViewModel
        {
            Beneficiarios = await beneficiaryService.GetByUserAsync(UsuarioActualId)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(AgregarBeneficiarioViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", new BeneficiariosIndexViewModel
            {
                Beneficiarios = await beneficiaryService.GetByUserAsync(UsuarioActualId),
                Nuevo = model
            });
        }

        var result = await beneficiaryService.AddAsync(UsuarioActualId, model.AccountNumber!);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
        }
        else
        {
            logger.LogInformation("Beneficiario agregado por el cliente {ClienteId}", UsuarioActualId);
            TempData["Mensaje"] = result.Message ?? Mensajes.BeneficiarioAgregado;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var result = await beneficiaryService.RemoveAsync(UsuarioActualId, id);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
        }
        else
        {
            logger.LogInformation("Beneficiario {BeneficiarioId} eliminado por el cliente {ClienteId}", id, UsuarioActualId);
            TempData["Mensaje"] = result.Message ?? Mensajes.BeneficiarioEliminado;
        }

        return RedirectToAction(nameof(Index));
    }
}
