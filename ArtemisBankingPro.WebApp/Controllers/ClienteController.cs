using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApp.ViewModels.Cliente;
using ArtemisBankingPro.WebApp.ViewModels.Cuentas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApp.Controllers;

[Authorize(Roles = Roles.Cliente)]
public class ClienteController(
    ISavingsAccountService savingsAccountService,
    ILoanService loanService,
    ICreditCardService creditCardService) : Controller
{
    private string UsuarioActualId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Home del cliente: sus productos financieros (solo los suyos).</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new ClienteHomeViewModel
        {
            Cuentas = await savingsAccountService.GetActiveAccountsByUserAsync(UsuarioActualId),
            Prestamos = await loanService.GetActiveLoansByUserAsync(UsuarioActualId),
            Tarjetas = await creditCardService.GetActiveCardsByUserAsync(UsuarioActualId)
        });
    }

    /// <summary>Detalle de una cuenta PROPIA con sus transacciones.</summary>
    [HttpGet]
    public async Task<IActionResult> DetalleCuenta(string id, int page = 1)
    {
        var cuenta = await savingsAccountService.GetByNumberAsync(id);
        if (cuenta is null || cuenta.UserId != UsuarioActualId)
            return RedirectToAction("AccesoDenegado", "Account");

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

    /// <summary>Detalle de un préstamo PROPIO con su tabla de amortización.</summary>
    [HttpGet]
    public async Task<IActionResult> DetallePrestamo(int id)
    {
        var result = await loanService.GetDetailAsync(id);
        if (!result.Succeeded || result.Data!.UserId != UsuarioActualId)
            return RedirectToAction("AccesoDenegado", "Account");

        return View(result.Data);
    }

    /// <summary>Detalle de una tarjeta PROPIA con sus consumos (número siempre enmascarado).</summary>
    [HttpGet]
    public async Task<IActionResult> DetalleTarjeta(int id)
    {
        var result = await creditCardService.GetDetailAsync(id);
        if (!result.Succeeded || result.Data!.UserId != UsuarioActualId)
            return RedirectToAction("AccesoDenegado", "Account");

        return View(result.Data);
    }
}
