using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApp.Helpers;
using ArtemisBankingPro.WebApp.ViewModels.Avance;
using ArtemisBankingPro.WebApp.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApp.Controllers;

[Authorize(Roles = Roles.Cliente)]
public class AvanceController(
    ICashAdvanceService cashAdvanceService,
    ICreditCardService creditCardService,
    ISavingsAccountService savingsAccountService,
    ILogger<AvanceController> logger) : Controller
{
    private string UsuarioActualId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await CargarAsync(new AvanceViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AvanceViewModel model)
    {
        if (!ModelState.IsValid)
            return View(await CargarAsync(model));

        var confirmacion = await cashAdvanceService.PrepareAsync(
            UsuarioActualId, model.CardId!.Value, model.DestinationAccountNumber!, model.Amount!.Value);

        if (!confirmacion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, confirmacion.Message!);
            return View(await CargarAsync(model));
        }

        var dto = confirmacion.Data!;
        var vm = new ConfirmarOperacionViewModel
        {
            Titulo = "Confirmar avance de efectivo",
            Pregunta = Mensajes.ConfirmarTransaccion,
            Accion = "Ejecutar",
            Controlador = "Avance"
        };
        vm.Agregar("Tarjeta (últimos 4)", dto.CardLastFourDigits);
        vm.Agregar("Cuenta de destino", dto.DestinationAccountNumber);
        vm.Agregar("Monto del avance", Formato.Monto(dto.AdvanceAmount));
        vm.Agregar("Interés (6.25%)", Formato.Monto(dto.InterestAmount));
        vm.Agregar("Total a cargar a la tarjeta", Formato.Monto(dto.TotalToCharge));
        vm.Agregar("Crédito disponible", Formato.Monto(dto.AvailableCredit));
        vm.Campos["cardId"] = model.CardId!.Value.ToString();
        vm.Campos["destinationAccountNumber"] = model.DestinationAccountNumber!;
        vm.Campos["amount"] = model.Amount!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return View("ConfirmarOperacion", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ejecutar(int cardId, string destinationAccountNumber, decimal amount)
    {
        var result = await cashAdvanceService.ExecuteAsync(
            UsuarioActualId, cardId, destinationAccountNumber, amount);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        logger.LogInformation("Avance de efectivo de {Monto} ejecutado por el cliente {ClienteId}",
            Formato.Monto(amount), UsuarioActualId);
        TempData["Mensaje"] = result.Message ?? "El avance de efectivo fue realizado correctamente.";
        return RedirectToAction("Index", "Cliente");
    }

    private async Task<AvanceViewModel> CargarAsync(AvanceViewModel model)
    {
        model.Tarjetas = await creditCardService.GetActiveCardsByUserAsync(UsuarioActualId);
        model.Cuentas = await savingsAccountService.GetActiveAccountsByUserAsync(UsuarioActualId);
        return model;
    }
}
