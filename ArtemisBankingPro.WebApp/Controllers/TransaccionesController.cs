using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Dtos.Transactions;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApp.Helpers;
using ArtemisBankingPro.WebApp.ViewModels.Shared;
using ArtemisBankingPro.WebApp.ViewModels.Transacciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApp.Controllers;

[Authorize(Roles = Roles.Cliente)]
public class TransaccionesController(
    ITransactionService transactionService,
    ISavingsAccountService savingsAccountService,
    ICreditCardService creditCardService,
    ILoanService loanService,
    IBeneficiaryService beneficiaryService,
    ILogger<TransaccionesController> logger) : Controller
{
    private string UsuarioActualId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // =============================================================
    // Transacción express
    // =============================================================

    [HttpGet]
    public async Task<IActionResult> Express()
    {
        return View(new ExpressViewModel
        {
            CuentasOrigen = await savingsAccountService.GetActiveAccountsByUserAsync(UsuarioActualId)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Express(ExpressViewModel model)
    {
        if (!ModelState.IsValid)
            return View(await RedisplayExpressAsync(model));

        var confirmacion = await transactionService.PrepareExpressTransferAsync(
            model.SourceAccountNumber!, model.DestinationAccountNumber!, model.Amount!.Value,
            ownerUserId: UsuarioActualId);

        if (!confirmacion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, confirmacion.Message!);
            return View(await RedisplayExpressAsync(model));
        }

        var vm = ConfirmacionTransferencia("Confirmar transacción express", confirmacion.Data!, "EjecutarExpress", "Express");
        vm.Campos["sourceAccountNumber"] = model.SourceAccountNumber!;
        vm.Campos["destinationAccountNumber"] = model.DestinationAccountNumber!;
        vm.Campos["amount"] = model.Amount!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return View("ConfirmarOperacion", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EjecutarExpress(string sourceAccountNumber, string destinationAccountNumber, decimal amount)
    {
        var result = await transactionService.ExpressTransferAsync(
            sourceAccountNumber, destinationAccountNumber, amount, ownerUserId: UsuarioActualId);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Express));
        }

        logger.LogInformation("Transacción express ejecutada por el cliente {ClienteId} por {Monto}",
            UsuarioActualId, Formato.Monto(amount));
        TempData["Mensaje"] = result.Message ?? "La transacción fue realizada correctamente.";
        return RedirectToAction("Index", "Cliente");
    }

    // =============================================================
    // Pago a tarjeta de crédito
    // =============================================================

    [HttpGet]
    public async Task<IActionResult> PagoTarjeta()
    {
        return View(await CargarPagoTarjetaAsync(new PagoTarjetaViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PagoTarjeta(PagoTarjetaViewModel model)
    {
        if (!ModelState.IsValid)
            return View(await CargarPagoTarjetaAsync(model));

        // El número completo se resuelve en el servidor: el cliente nunca lo digita ni lo ve.
        var cardNumber = await creditCardService.GetCardNumberForOwnerAsync(model.CardId!.Value, UsuarioActualId);
        if (cardNumber is null)
        {
            ModelState.AddModelError(string.Empty, Mensajes.TarjetaNoExiste);
            return View(await CargarPagoTarjetaAsync(model));
        }

        var confirmacion = await transactionService.PrepareCreditCardPaymentAsync(
            model.SourceAccountNumber!, cardNumber, model.Amount!.Value, ownerUserId: UsuarioActualId);

        if (!confirmacion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, confirmacion.Message!);
            return View(await CargarPagoTarjetaAsync(model));
        }

        var dto = confirmacion.Data!;
        var vm = new ConfirmarOperacionViewModel
        {
            Titulo = "Confirmar pago a tarjeta de crédito",
            Pregunta = Mensajes.ConfirmarPago,
            Accion = "EjecutarPagoTarjeta",
            Controlador = "Transacciones",
            CancelarAccion = "PagoTarjeta"
        };
        vm.Agregar("Cuenta de origen", dto.SourceAccountNumber);
        vm.Agregar("Titular de la cuenta", dto.SourceHolderFullName);
        vm.Agregar("Tarjeta (últimos 4)", dto.ProductReference);
        vm.Agregar("Titular de la tarjeta", dto.DestinationHolderFullName);
        vm.Agregar("Monto ingresado", Formato.Monto(dto.RequestedAmount));
        vm.Agregar("Monto efectivo a debitar", Formato.Monto(dto.EffectiveAmount));
        vm.Campos["sourceAccountNumber"] = model.SourceAccountNumber!;
        vm.Campos["cardId"] = model.CardId!.Value.ToString();
        vm.Campos["amount"] = model.Amount!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return View("ConfirmarOperacion", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EjecutarPagoTarjeta(string sourceAccountNumber, int cardId, decimal amount)
    {
        var cardNumber = await creditCardService.GetCardNumberForOwnerAsync(cardId, UsuarioActualId);
        if (cardNumber is null)
        {
            TempData["Error"] = Mensajes.TarjetaNoExiste;
            return RedirectToAction(nameof(PagoTarjeta));
        }

        var result = await transactionService.PayCreditCardAsync(
            sourceAccountNumber, cardNumber, amount, ownerUserId: UsuarioActualId);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(PagoTarjeta));
        }

        logger.LogInformation("Pago a tarjeta terminada en {UltimosCuatro} ejecutado por el cliente {ClienteId}",
            cardNumber[^4..], UsuarioActualId);
        TempData["Mensaje"] = result.Message ?? "El pago fue realizado correctamente.";
        return RedirectToAction("Index", "Cliente");
    }

    // =============================================================
    // Pago a préstamo
    // =============================================================

    [HttpGet]
    public async Task<IActionResult> PagoPrestamo()
    {
        return View(await CargarPagoPrestamoAsync(new PagoPrestamoViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PagoPrestamo(PagoPrestamoViewModel model)
    {
        if (!ModelState.IsValid)
            return View(await CargarPagoPrestamoAsync(model));

        var confirmacion = await transactionService.PrepareLoanPaymentAsync(
            model.SourceAccountNumber!, model.LoanNumber!, model.Amount!.Value, ownerUserId: UsuarioActualId);

        if (!confirmacion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, confirmacion.Message!);
            return View(await CargarPagoPrestamoAsync(model));
        }

        var dto = confirmacion.Data!;
        var vm = new ConfirmarOperacionViewModel
        {
            Titulo = "Confirmar pago a préstamo",
            Pregunta = Mensajes.ConfirmarPago,
            Accion = "EjecutarPagoPrestamo",
            Controlador = "Transacciones",
            CancelarAccion = "PagoPrestamo"
        };
        vm.Agregar("Cuenta de origen", dto.SourceAccountNumber);
        vm.Agregar("Titular de la cuenta", dto.SourceHolderFullName);
        vm.Agregar("Préstamo", dto.ProductReference);
        vm.Agregar("Titular del préstamo", dto.DestinationHolderFullName);
        vm.Agregar("Monto ingresado", Formato.Monto(dto.RequestedAmount));
        vm.Agregar("Monto efectivo a debitar", Formato.Monto(dto.EffectiveAmount));
        vm.Campos["sourceAccountNumber"] = model.SourceAccountNumber!;
        vm.Campos["loanNumber"] = model.LoanNumber!;
        vm.Campos["amount"] = model.Amount!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return View("ConfirmarOperacion", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EjecutarPagoPrestamo(string sourceAccountNumber, string loanNumber, decimal amount)
    {
        var result = await transactionService.PayLoanAsync(
            sourceAccountNumber, loanNumber, amount, ownerUserId: UsuarioActualId);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(PagoPrestamo));
        }

        logger.LogInformation("Pago al préstamo {Prestamo} ejecutado por el cliente {ClienteId}",
            loanNumber, UsuarioActualId);
        TempData["Mensaje"] = result.Message ?? "El pago fue realizado correctamente.";
        return RedirectToAction("Index", "Cliente");
    }

    // =============================================================
    // Transacción a beneficiarios
    // =============================================================

    [HttpGet]
    public async Task<IActionResult> PagoBeneficiario()
    {
        return View(await CargarPagoBeneficiarioAsync(new PagoBeneficiarioViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PagoBeneficiario(PagoBeneficiarioViewModel model)
    {
        if (!ModelState.IsValid)
            return View(await CargarPagoBeneficiarioAsync(model));

        var confirmacion = await transactionService.PrepareTransferToBeneficiaryAsync(
            UsuarioActualId, model.BeneficiaryId!.Value, model.SourceAccountNumber!, model.Amount!.Value);

        if (!confirmacion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, confirmacion.Message!);
            return View(await CargarPagoBeneficiarioAsync(model));
        }

        var vm = ConfirmacionTransferencia("Confirmar transacción a beneficiario", confirmacion.Data!, "EjecutarPagoBeneficiario", "PagoBeneficiario");
        vm.Campos["beneficiaryId"] = model.BeneficiaryId!.Value.ToString();
        vm.Campos["sourceAccountNumber"] = model.SourceAccountNumber!;
        vm.Campos["amount"] = model.Amount!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return View("ConfirmarOperacion", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EjecutarPagoBeneficiario(int beneficiaryId, string sourceAccountNumber, decimal amount)
    {
        var result = await transactionService.TransferToBeneficiaryAsync(
            UsuarioActualId, beneficiaryId, sourceAccountNumber, amount);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(PagoBeneficiario));
        }

        logger.LogInformation("Transacción a beneficiario {BeneficiarioId} ejecutada por el cliente {ClienteId}",
            beneficiaryId, UsuarioActualId);
        TempData["Mensaje"] = result.Message ?? "La transacción fue realizada correctamente.";
        return RedirectToAction("Index", "Cliente");
    }

    // =============================================================
    // Helpers
    // =============================================================

    private ConfirmarOperacionViewModel ConfirmacionTransferencia(
        string titulo, TransactionConfirmationDto dto, string accion, string cancelarAccion)
    {
        var vm = new ConfirmarOperacionViewModel
        {
            Titulo = titulo,
            Pregunta = Mensajes.ConfirmarTransaccion,
            Accion = accion,
            Controlador = "Transacciones",
            CancelarAccion = cancelarAccion
        };
        vm.Agregar("Cuenta de origen", dto.SourceAccountNumber);
        vm.Agregar("Titular de origen", dto.SourceHolderFullName);
        vm.Agregar("Cuenta de destino", dto.DestinationAccountNumber);
        vm.Agregar("Titular de destino", dto.DestinationHolderFullName);
        vm.Agregar("Monto", Formato.Monto(dto.EffectiveAmount));
        return vm;
    }

    private async Task<ExpressViewModel> RedisplayExpressAsync(ExpressViewModel model)
    {
        model.CuentasOrigen = await savingsAccountService.GetActiveAccountsByUserAsync(UsuarioActualId);
        return model;
    }

    private async Task<PagoTarjetaViewModel> CargarPagoTarjetaAsync(PagoTarjetaViewModel model)
    {
        model.CuentasOrigen = await savingsAccountService.GetActiveAccountsByUserAsync(UsuarioActualId);
        model.Tarjetas = (await creditCardService.GetActiveCardsByUserAsync(UsuarioActualId))
            .Where(t => t.OwedAmount > 0)
            .ToList();
        return model;
    }

    private async Task<PagoPrestamoViewModel> CargarPagoPrestamoAsync(PagoPrestamoViewModel model)
    {
        model.CuentasOrigen = await savingsAccountService.GetActiveAccountsByUserAsync(UsuarioActualId);
        model.Prestamos = await loanService.GetActiveLoansByUserAsync(UsuarioActualId);
        return model;
    }

    private async Task<PagoBeneficiarioViewModel> CargarPagoBeneficiarioAsync(PagoBeneficiarioViewModel model)
    {
        model.CuentasOrigen = await savingsAccountService.GetActiveAccountsByUserAsync(UsuarioActualId);
        model.Beneficiarios = await beneficiaryService.GetByUserAsync(UsuarioActualId);
        return model;
    }
}
