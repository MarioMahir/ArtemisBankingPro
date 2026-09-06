using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.WebApp.ViewModels.Cuentas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApp.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class CuentasController(
    ISavingsAccountService savingsAccountService,
    IAccountService accountService,
    ILogger<CuentasController> logger) : Controller
{
    private string UsuarioActualId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string estado = "Activas", string tipo = "Todas", string? cedula = null)
    {
        ProductStatus? status = estado switch
        {
            "Canceladas" => ProductStatus.Cancelada,
            "Todas" => null,
            _ => ProductStatus.Activa
        };

        AccountType? type = tipo switch
        {
            "Principal" => AccountType.Principal,
            "Secundaria" => AccountType.Secundaria,
            _ => null
        };

        var result = await savingsAccountService.GetPagedAsync(page, status, type, cedula);
        var vm = new CuentasIndexViewModel { Estado = estado, Tipo = tipo, Cedula = cedula };

        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return View(vm);
        }

        vm.Cuentas = result.Data!;
        return View(vm);
    }

    // ---------------- Asignar (solo secundarias) ----------------

    [HttpGet]
    public async Task<IActionResult> Asignar()
    {
        return View(new AsignarCuentaViewModel
        {
            Clientes = await accountService.GetActiveClientsAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(AsignarCuentaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Clientes = await accountService.GetActiveClientsAsync();
            return View(model);
        }

        var result = await savingsAccountService.CreateSecondaryAsync(model.ClientId!, model.InitialBalance, UsuarioActualId);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            model.Clientes = await accountService.GetActiveClientsAsync();
            return View(model);
        }

        logger.LogInformation(
            "Cuenta secundaria {Cuenta} asignada al cliente {ClienteId} con balance inicial RD${Balance:#,##0.00}",
            result.Data!.AccountNumber, model.ClientId, model.InitialBalance);

        TempData["Mensaje"] = "La cuenta de ahorro fue asignada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- Detalle (transacciones) ----------------

    [HttpGet]
    public async Task<IActionResult> Detalle(string id, int page = 1)
    {
        var cuenta = await savingsAccountService.GetByNumberAsync(id);
        if (cuenta is null)
        {
            TempData["Error"] = Mensajes.CuentaNoExiste;
            return RedirectToAction(nameof(Index));
        }

        var transacciones = await savingsAccountService.GetTransactionsAsync(id, page);
        if (!transacciones.Succeeded)
        {
            TempData["Error"] = transacciones.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(new DetalleCuentaViewModel
        {
            Cuenta = cuenta,
            Transacciones = transacciones.Data!
        });
    }

    // ---------------- Cancelar (solo secundarias) ----------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(string id)
    {
        var result = await savingsAccountService.CancelAsync(id);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
        }
        else
        {
            logger.LogInformation("Cuenta de ahorro {Cuenta} cancelada por el administrador", id);
            TempData["Mensaje"] = "La cuenta fue cancelada correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }
}
