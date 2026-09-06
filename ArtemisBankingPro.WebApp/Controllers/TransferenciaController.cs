using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApp.Helpers;
using ArtemisBankingPro.WebApp.ViewModels.Shared;
using ArtemisBankingPro.WebApp.ViewModels.Transferencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApp.Controllers;

[Authorize(Roles = Roles.Cliente)]
public class TransferenciaController(
    ITransactionService transactionService,
    ISavingsAccountService savingsAccountService,
    ILogger<TransferenciaController> logger) : Controller
{
    private string UsuarioActualId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cuentas = await savingsAccountService.GetActiveAccountsByUserAsync(UsuarioActualId);
        if (cuentas.Count < 2)
        {
            TempData["Error"] = Mensajes.RequiereDosCuentas;
            return RedirectToAction("Index", "Cliente");
        }

        return View(new TransferenciaViewModel { Cuentas = cuentas });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(TransferenciaViewModel model)
    {
        model.Cuentas = await savingsAccountService.GetActiveAccountsByUserAsync(UsuarioActualId);
        if (!ModelState.IsValid)
            return View(model);

        var confirmacion = await transactionService.PrepareTransferBetweenOwnAccountsAsync(
            UsuarioActualId, model.SourceAccountNumber!, model.DestinationAccountNumber!, model.Amount!.Value);

        if (!confirmacion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, confirmacion.Message!);
            return View(model);
        }

        var dto = confirmacion.Data!;
        var vm = new ConfirmarOperacionViewModel
        {
            Titulo = "Confirmar transferencia entre cuentas",
            Pregunta = Mensajes.ConfirmarTransferencia,
            Accion = "Ejecutar",
            Controlador = "Transferencia"
        };
        vm.Agregar("Cuenta de origen", dto.SourceAccountNumber);
        vm.Agregar("Cuenta de destino", dto.DestinationAccountNumber);
        vm.Agregar("Titular", dto.SourceHolderFullName);
        vm.Agregar("Monto", Formato.Monto(dto.EffectiveAmount));
        vm.Campos["sourceAccountNumber"] = model.SourceAccountNumber!;
        vm.Campos["destinationAccountNumber"] = model.DestinationAccountNumber!;
        vm.Campos["amount"] = model.Amount!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return View("ConfirmarOperacion", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ejecutar(string sourceAccountNumber, string destinationAccountNumber, decimal amount)
    {
        var result = await transactionService.TransferBetweenOwnAccountsAsync(
            UsuarioActualId, sourceAccountNumber, destinationAccountNumber, amount);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        logger.LogInformation("Transferencia entre cuentas propias de {Monto} ejecutada por el cliente {ClienteId}",
            Formato.Monto(amount), UsuarioActualId);
        TempData["Mensaje"] = result.Message ?? "La transferencia fue realizada correctamente.";
        return RedirectToAction("Index", "Cliente");
    }
}
