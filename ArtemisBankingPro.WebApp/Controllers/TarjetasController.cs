using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.WebApp.ViewModels.Tarjetas;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApp.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class TarjetasController(
    ICreditCardService creditCardService,
    IAccountService accountService,
    IMapper mapper,
    ILogger<TarjetasController> logger) : Controller
{
    private string UsuarioActualId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string estado = "Activas", string? cedula = null)
    {
        ProductStatus? status = estado switch
        {
            "Canceladas" => ProductStatus.Cancelada,
            "Todas" => null,
            _ => ProductStatus.Activa
        };

        var result = await creditCardService.GetPagedAsync(page, status, cedula);
        var vm = new TarjetasIndexViewModel { Estado = estado, Cedula = cedula };

        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return View(vm);
        }

        vm.Tarjetas = result.Data!;
        return View(vm);
    }

    // ---------------- Asignar: paso 1 (cliente) ----------------

    [HttpGet]
    public async Task<IActionResult> Asignar()
    {
        return View(new SeleccionarClienteTarjetaViewModel
        {
            Clientes = await accountService.GetActiveClientsAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(SeleccionarClienteTarjetaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Clientes = await accountService.GetActiveClientsAsync();
            return View(model);
        }

        return RedirectToAction(nameof(Limite), new { clientId = model.ClientId });
    }

    // ---------------- Asignar: paso 2 (límite) ----------------

    [HttpGet]
    public async Task<IActionResult> Limite(string clientId)
    {
        var cliente = await accountService.GetByIdAsync(clientId);
        if (cliente is null)
        {
            TempData["Error"] = Mensajes.DebeSeleccionarCliente;
            return RedirectToAction(nameof(Asignar));
        }

        return View(mapper.Map<AsignarTarjetaViewModel>(cliente));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Limite(AsignarTarjetaViewModel model)
    {
        if (!ModelState.IsValid)
            return await RedisplayLimiteAsync(model);

        var result = await creditCardService.AssignAsync(model.ClientId, model.CreditLimit!.Value, UsuarioActualId);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            return await RedisplayLimiteAsync(model);
        }

        logger.LogInformation(
            "Tarjeta terminada en {UltimosCuatro} asignada al cliente {ClienteId} con límite RD${Limite:#,##0.00}",
            result.Data!.LastFourDigits, model.ClientId, model.CreditLimit);

        TempData["Mensaje"] = result.Message ?? "La tarjeta de crédito fue asignada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- Detalle (consumos) ----------------

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var result = await creditCardService.GetDetailAsync(id);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // ---------------- Editar límite ----------------

    [HttpGet]
    public async Task<IActionResult> EditarLimite(int id)
    {
        var result = await creditCardService.GetDetailAsync(id);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(mapper.Map<EditarLimiteViewModel>(result.Data!));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarLimite(EditarLimiteViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await creditCardService.UpdateLimitAsync(model.CardId, model.NuevoLimite!.Value);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            return View(model);
        }

        logger.LogInformation(
            "Límite de la tarjeta terminada en {UltimosCuatro} actualizado a RD${Limite:#,##0.00}",
            model.UltimosCuatro, model.NuevoLimite);

        TempData["Mensaje"] = "El límite de la tarjeta fue actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- Cancelar ----------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var result = await creditCardService.CancelAsync(id);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
        }
        else
        {
            logger.LogInformation("Tarjeta {TarjetaId} cancelada por el administrador", id);
            TempData["Mensaje"] = "La tarjeta fue cancelada correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> RedisplayLimiteAsync(AsignarTarjetaViewModel model)
    {
        var cliente = await accountService.GetByIdAsync(model.ClientId);
        model.ClienteNombre = cliente?.FullName;
        model.ClienteCedula = cliente?.Identification;
        return View(model);
    }
}
