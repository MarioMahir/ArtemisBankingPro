using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Helpers;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.WebApp.ViewModels.Prestamos;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApp.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class PrestamosController(
    ILoanService loanService,
    IUserService userService,
    IMapper mapper,
    ILogger<PrestamosController> logger) : Controller
{
    private string UsuarioActualId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string estado = "Activos", string? cedula = null)
    {
        LoanStatus? status = estado switch
        {
            "Completados" => LoanStatus.Completado,
            "Todos" => null,
            _ => LoanStatus.Activo
        };

        var result = await loanService.GetPagedAsync(page, status, cedula);
        var vm = new PrestamosIndexViewModel { Estado = estado, Cedula = cedula };

        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return View(vm);
        }

        vm.Prestamos = result.Data!;
        return View(vm);
    }

    // ---------------- Asignar: paso 1 (selección de cliente) ----------------

    [HttpGet]
    public async Task<IActionResult> Asignar()
    {
        return View(new SeleccionarClienteViewModel
        {
            Clientes = await loanService.GetEligibleClientsAsync(),
            DeudaPromedio = await loanService.GetAverageDebtAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(SeleccionarClienteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Clientes = await loanService.GetEligibleClientsAsync();
            model.DeudaPromedio = await loanService.GetAverageDebtAsync();
            return View(model);
        }

        return RedirectToAction(nameof(DatosPrestamo), new { clientId = model.ClientId });
    }

    // ---------------- Asignar: paso 2 (plazo, monto y tasa) ----------------

    [HttpGet]
    public async Task<IActionResult> DatosPrestamo(string clientId)
    {
        var cliente = await userService.GetByIdAsync(clientId);
        if (cliente is null)
        {
            TempData["Error"] = Mensajes.DebeSeleccionarCliente;
            return RedirectToAction(nameof(Asignar));
        }

        return View(mapper.Map<DatosPrestamoViewModel>(cliente));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DatosPrestamo(DatosPrestamoViewModel model)
    {
        if (!ModelState.IsValid)
            return await RedisplayDatosAsync(model);

        var request = mapper.Map<CreateLoanRequest>(model);
        var riesgo = await loanService.EvaluateRiskAsync(request);
        if (!riesgo.Succeeded)
        {
            ModelState.AddModelError(string.Empty, riesgo.Message!);
            return await RedisplayDatosAsync(model);
        }

        // Alto riesgo (actual o proyectado): pantalla de advertencia con Cancelar/Confirmar.
        if (riesgo.Data!.RiskLevel != RiskLevel.None)
        {
            var cliente = await userService.GetByIdAsync(model.ClientId);
            return View("ConfirmarRiesgo", new ConfirmarRiesgoViewModel
            {
                ClientId = model.ClientId,
                ClienteNombre = cliente?.FullName,
                TermInMonths = model.TermInMonths!.Value,
                CapitalAmount = model.CapitalAmount!.Value,
                AnnualInterestRate = model.AnnualInterestRate!.Value,
                Advertencia = riesgo.Data.WarningMessage!,
                DeudaActual = riesgo.Data.CurrentDebt,
                DeudaProyectada = riesgo.Data.ProjectedDebt,
                DeudaPromedio = riesgo.Data.AverageDebt,
                TotalAPagarNuevoPrestamo = riesgo.Data.NewLoanTotalToPay
            });
        }

        return await CrearPrestamoAsync(request, model);
    }

    /// <summary>Confirmación tras la advertencia de alto riesgo.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarAsignacion(ConfirmarRiesgoViewModel model)
    {
        var request = mapper.Map<CreateLoanRequest>(model);
        return await CrearPrestamoAsync(request, null);
    }

    // ---------------- Detalle (amortización) ----------------

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var result = await loanService.GetDetailAsync(id);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // ---------------- Editar tasa ----------------

    [HttpGet]
    public async Task<IActionResult> EditarTasa(int id)
    {
        var result = await loanService.GetDetailAsync(id);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(mapper.Map<EditarTasaViewModel>(result.Data!));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarTasa(EditarTasaViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await loanService.UpdateRateAsync(model.LoanId, model.NuevaTasa!.Value);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            return View(model);
        }

        logger.LogInformation("Tasa del préstamo {Prestamo} actualizada a {Tasa}%", model.LoanNumber, model.NuevaTasa);
        TempData["Mensaje"] = "La tasa de interés fue actualizada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- Helpers ----------------

    private async Task<IActionResult> CrearPrestamoAsync(CreateLoanRequest request, DatosPrestamoViewModel? redisplay)
    {
        var result = await loanService.CreateLoanAsync(request, UsuarioActualId);
        if (!result.Succeeded)
        {
            if (redisplay is not null)
            {
                ModelState.AddModelError(string.Empty, result.Message!);
                return await RedisplayDatosAsync(redisplay);
            }

            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Asignar));
        }

        logger.LogInformation(
            "Préstamo {Prestamo} asignado al cliente {ClienteId} por RD${Monto:#,##0.00} a {Plazo} meses",
            result.Data!.LoanNumber, request.ClientId, request.CapitalAmount, request.TermInMonths);

        TempData["Mensaje"] = result.Message ?? "El préstamo fue asignado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> RedisplayDatosAsync(DatosPrestamoViewModel model)
    {
        var cliente = await userService.GetByIdAsync(model.ClientId);
        model.ClienteNombre = cliente?.FullName;
        model.ClienteCedula = cliente?.Identification;
        return View(model);
    }
}
