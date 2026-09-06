using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApp.Helpers;
using ArtemisBankingPro.WebApp.ViewModels.Cajero;
using ArtemisBankingPro.WebApp.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApp.Controllers;

[Authorize(Roles = Roles.Cajero)]
public class CajeroController(
    IDashboardService dashboardService,
    ITransactionService transactionService,
    ILogger<CajeroController> logger) : Controller
{
    private string CajeroId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Home del cajero: sus 4 indicadores de HOY.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await dashboardService.GetCashierDashboardAsync(CajeroId));
    }

    // =============================================================
    // Depósito
    // =============================================================

    [HttpGet]
    public IActionResult Deposito() => View(new DepositoViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deposito(DepositoViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var confirmacion = await transactionService.PrepareDepositAsync(model.AccountNumber!, model.Amount!.Value);
        if (!confirmacion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, confirmacion.Message!);
            return View(model);
        }

        var dto = confirmacion.Data!;
        var vm = new ConfirmarOperacionViewModel
        {
            Titulo = "Confirmar depósito",
            Pregunta = Mensajes.ConfirmarDeposito,
            Accion = "EjecutarDeposito",
            Controlador = "Cajero",
            CancelarAccion = "Deposito"
        };
        vm.Agregar("Cuenta de destino", dto.DestinationAccountNumber);
        vm.Agregar("Titular", dto.DestinationHolderFullName);
        vm.Agregar("Monto a depositar", Formato.Monto(dto.EffectiveAmount));
        vm.Campos["accountNumber"] = model.AccountNumber!;
        vm.Campos["amount"] = model.Amount!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return View("ConfirmarOperacion", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EjecutarDeposito(string accountNumber, decimal amount)
    {
        var result = await transactionService.DepositAsync(accountNumber, amount, CajeroId);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Deposito));
        }

        logger.LogInformation("Depósito de {Monto} a la cuenta {Cuenta} realizado por el cajero {CajeroId}",
            Formato.Monto(amount), accountNumber, CajeroId);
        TempData["Mensaje"] = result.Message ?? "El depósito fue realizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // =============================================================
    // Retiro
    // =============================================================

    [HttpGet]
    public IActionResult Retiro() => View(new RetiroViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retiro(RetiroViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var confirmacion = await transactionService.PrepareWithdrawAsync(model.AccountNumber!, model.Amount!.Value);
        if (!confirmacion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, confirmacion.Message!);
            return View(model);
        }

        var dto = confirmacion.Data!;
        var vm = new ConfirmarOperacionViewModel
        {
            Titulo = "Confirmar retiro",
            Pregunta = Mensajes.ConfirmarRetiro,
            Accion = "EjecutarRetiro",
            Controlador = "Cajero",
            CancelarAccion = "Retiro"
        };
        vm.Agregar("Cuenta", dto.SourceAccountNumber);
        vm.Agregar("Titular", dto.SourceHolderFullName);
        vm.Agregar("Monto a retirar", Formato.Monto(dto.EffectiveAmount));
        vm.Campos["accountNumber"] = model.AccountNumber!;
        vm.Campos["amount"] = model.Amount!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return View("ConfirmarOperacion", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EjecutarRetiro(string accountNumber, decimal amount)
    {
        var result = await transactionService.WithdrawAsync(accountNumber, amount, CajeroId);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Retiro));
        }

        logger.LogInformation("Retiro de {Monto} de la cuenta {Cuenta} realizado por el cajero {CajeroId}",
            Formato.Monto(amount), accountNumber, CajeroId);
        TempData["Mensaje"] = result.Message ?? "El retiro fue realizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // =============================================================
    // Pago a tarjeta de crédito (números manuales)
    // =============================================================

    [HttpGet]
    public IActionResult PagoTarjeta() => View(new PagoTarjetaCajeroViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PagoTarjeta(PagoTarjetaCajeroViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var confirmacion = await transactionService.PrepareCreditCardPaymentAsync(
            model.AccountNumber!, model.CardNumber!, model.Amount!.Value, cashierId: CajeroId);

        if (!confirmacion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, confirmacion.Message!);
            return View(model);
        }

        var dto = confirmacion.Data!;
        var vm = new ConfirmarOperacionViewModel
        {
            Titulo = "Confirmar pago a tarjeta de crédito",
            Pregunta = Mensajes.ConfirmarPago,
            Accion = "EjecutarPagoTarjeta",
            Controlador = "Cajero",
            CancelarAccion = "PagoTarjeta"
        };
        vm.Agregar("Cuenta de origen", dto.SourceAccountNumber);
        vm.Agregar("Titular de la cuenta", dto.SourceHolderFullName);
        vm.Agregar("Tarjeta (últimos 4)", dto.ProductReference);
        vm.Agregar("Titular de la tarjeta", dto.DestinationHolderFullName);
        vm.Agregar("Monto ingresado", Formato.Monto(dto.RequestedAmount));
        vm.Agregar("Monto efectivo a debitar", Formato.Monto(dto.EffectiveAmount));
        vm.Campos["accountNumber"] = model.AccountNumber!;
        vm.Campos["cardNumber"] = model.CardNumber!;
        vm.Campos["amount"] = model.Amount!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return View("ConfirmarOperacion", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EjecutarPagoTarjeta(string accountNumber, string cardNumber, decimal amount)
    {
        var result = await transactionService.PayCreditCardAsync(accountNumber, cardNumber, amount, cashierId: CajeroId);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(PagoTarjeta));
        }

        logger.LogInformation("Pago a tarjeta terminada en {UltimosCuatro} realizado por el cajero {CajeroId}",
            cardNumber.Length >= 4 ? cardNumber[^4..] : cardNumber, CajeroId);
        TempData["Mensaje"] = result.Message ?? "El pago fue realizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // =============================================================
    // Pago a préstamo (números manuales)
    // =============================================================

    [HttpGet]
    public IActionResult PagoPrestamo() => View(new PagoPrestamoCajeroViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PagoPrestamo(PagoPrestamoCajeroViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var confirmacion = await transactionService.PrepareLoanPaymentAsync(
            model.AccountNumber!, model.LoanNumber!, model.Amount!.Value, cashierId: CajeroId);

        if (!confirmacion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, confirmacion.Message!);
            return View(model);
        }

        var dto = confirmacion.Data!;
        var vm = new ConfirmarOperacionViewModel
        {
            Titulo = "Confirmar pago a préstamo",
            Pregunta = Mensajes.ConfirmarPago,
            Accion = "EjecutarPagoPrestamo",
            Controlador = "Cajero",
            CancelarAccion = "PagoPrestamo"
        };
        vm.Agregar("Cuenta de origen", dto.SourceAccountNumber);
        vm.Agregar("Titular de la cuenta", dto.SourceHolderFullName);
        vm.Agregar("Préstamo", dto.ProductReference);
        vm.Agregar("Titular del préstamo", dto.DestinationHolderFullName);
        vm.Agregar("Monto ingresado", Formato.Monto(dto.RequestedAmount));
        vm.Agregar("Monto efectivo a debitar", Formato.Monto(dto.EffectiveAmount));
        vm.Campos["accountNumber"] = model.AccountNumber!;
        vm.Campos["loanNumber"] = model.LoanNumber!;
        vm.Campos["amount"] = model.Amount!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return View("ConfirmarOperacion", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EjecutarPagoPrestamo(string accountNumber, string loanNumber, decimal amount)
    {
        var result = await transactionService.PayLoanAsync(accountNumber, loanNumber, amount, cashierId: CajeroId);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(PagoPrestamo));
        }

        logger.LogInformation("Pago al préstamo {Prestamo} realizado por el cajero {CajeroId}", loanNumber, CajeroId);
        TempData["Mensaje"] = result.Message ?? "El pago fue realizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // =============================================================
    // Transacciones a cuentas de terceros
    // =============================================================

    [HttpGet]
    public IActionResult Terceros() => View(new TercerosViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Terceros(TercerosViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var confirmacion = await transactionService.PrepareExpressTransferAsync(
            model.SourceAccountNumber!, model.DestinationAccountNumber!, model.Amount!.Value, cashierId: CajeroId);

        if (!confirmacion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, confirmacion.Message!);
            return View(model);
        }

        var dto = confirmacion.Data!;
        var vm = new ConfirmarOperacionViewModel
        {
            Titulo = "Confirmar transacción a terceros",
            Pregunta = Mensajes.ConfirmarTransaccion,
            Accion = "EjecutarTerceros",
            Controlador = "Cajero",
            CancelarAccion = "Terceros"
        };
        vm.Agregar("Cuenta de origen", dto.SourceAccountNumber);
        vm.Agregar("Titular de origen", dto.SourceHolderFullName);
        vm.Agregar("Cuenta de destino", dto.DestinationAccountNumber);
        vm.Agregar("Titular de destino", dto.DestinationHolderFullName);
        vm.Agregar("Monto", Formato.Monto(dto.EffectiveAmount));
        vm.Campos["sourceAccountNumber"] = model.SourceAccountNumber!;
        vm.Campos["destinationAccountNumber"] = model.DestinationAccountNumber!;
        vm.Campos["amount"] = model.Amount!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return View("ConfirmarOperacion", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EjecutarTerceros(string sourceAccountNumber, string destinationAccountNumber, decimal amount)
    {
        var result = await transactionService.ExpressTransferAsync(
            sourceAccountNumber, destinationAccountNumber, amount, cashierId: CajeroId);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Terceros));
        }

        logger.LogInformation("Transacción a terceros de {Monto} realizada por el cajero {CajeroId}",
            Formato.Monto(amount), CajeroId);
        TempData["Mensaje"] = result.Message ?? "La transacción fue realizada correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
