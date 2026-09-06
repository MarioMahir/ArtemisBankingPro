using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApp.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApp.Controllers;

[AllowAnonymous]
public class AccountController(IAccountService accountService, ILogger<AccountController> logger) : Controller
{
    // ---------------- Login / Logout ----------------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirigirAlHomeDelRol();

        // Redirigido por el middleware de cookies al intentar acceder sin autenticarse.
        if (!string.IsNullOrWhiteSpace(returnUrl))
            TempData["Error"] = Mensajes.NoAutenticado;

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirigirAlHomeDelRol();

        if (!ModelState.IsValid)
            return View(model);

        var result = await accountService.AuthenticateAsync(model.UserName, model.Password, Roles.WebAppRoles);
        if (!result.Succeeded)
        {
            logger.LogWarning("Inicio de sesión fallido para el usuario {UsuarioIntento}", model.UserName);
            ModelState.AddModelError(string.Empty, result.Message!);
            return View(model);
        }

        var user = result.Data!;
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role)
        ], IdentityConstants.ApplicationScheme);

        await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, new ClaimsPrincipal(identity));

        logger.LogInformation("Inicio de sesión correcto del usuario {Usuario} con rol {Rol}", user.UserName, user.Role);
        return RedirigirAlHomeDelRol(user.Role);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        logger.LogInformation("Cierre de sesión del usuario {Usuario}", User.Identity?.Name);
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return RedirectToAction(nameof(Login));
    }

    // ---------------- Activación de cuenta ----------------

    [HttpGet]
    public async Task<IActionResult> Activar(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["Error"] = Mensajes.EnlaceActivacionInvalido;
            return RedirectToAction(nameof(Login));
        }

        var result = await accountService.ConfirmAccountAsync(token);
        if (result.Succeeded)
        {
            logger.LogInformation("Cuenta activada correctamente mediante enlace de activación");
            TempData["Mensaje"] = result.Message ?? Mensajes.CuentaActivada;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Login));
    }

    // ---------------- Restablecimiento de contraseña ----------------

    [HttpGet]
    public IActionResult SolicitarReset() => View(new SolicitarResetViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SolicitarReset(SolicitarResetViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await accountService.RequestPasswordResetAsync(model.UserName, tokenInBody: false, Roles.WebAppRoles);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            return View(model);
        }

        logger.LogInformation("Solicitud de restablecimiento de contraseña procesada para {Usuario}", model.UserName);
        TempData["Mensaje"] = result.Message ?? Mensajes.ResetEnviado;
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Restablecer(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            TempData["Error"] = Mensajes.EnlaceResetInvalido;
            return RedirectToAction(nameof(Login));
        }

        return View(new RestablecerViewModel { UserId = userId, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restablecer(RestablecerViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await accountService.ResetPasswordAsync(model.UserId, model.Token, model.Password, model.ConfirmPassword);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            return View(model);
        }

        logger.LogInformation("Contraseña restablecida correctamente");
        TempData["Mensaje"] = result.Message ?? Mensajes.ContrasenaRestablecida;
        return RedirectToAction(nameof(Login));
    }

    // ---------------- Acceso denegado ----------------

    [HttpGet]
    public IActionResult AccesoDenegado() => View();

    // ---------------- Helpers ----------------

    private IActionResult RedirigirAlHomeDelRol(string? role = null)
    {
        role ??= User.FindFirstValue(ClaimTypes.Role);
        return role switch
        {
            Roles.Administrador => RedirectToAction("Index", "Admin"),
            Roles.Cajero => RedirectToAction("Index", "Cajero"),
            Roles.Cliente => RedirectToAction("Index", "Cliente"),
            _ => RedirectToAction(nameof(Login))
        };
    }
}
