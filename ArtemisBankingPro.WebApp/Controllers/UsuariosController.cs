using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApp.ViewModels.Usuarios;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApp.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class UsuariosController(
    IUserService userService,
    IMapper mapper,
    ILogger<UsuariosController> logger) : Controller
{
    private string UsuarioActualId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? rol = null)
    {
        var usuarios = await userService.GetPagedAsync(page, rol);
        return View(new UsuariosIndexViewModel
        {
            Usuarios = usuarios,
            Rol = rol,
            UsuarioActualId = UsuarioActualId
        });
    }

    // ---------------- Crear ----------------

    [HttpGet]
    public IActionResult Crear() => View(new CrearUsuarioViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearUsuarioViewModel model)
    {
        if (model.Role != Roles.Cliente)
            model.InitialAmount = null;

        if (!ModelState.IsValid)
            return View(model);

        var request = mapper.Map<CreateUserRequest>(model);
        var result = await userService.CreateUserAsync(request);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            return View(model);
        }

        logger.LogInformation("Usuario {Usuario} creado con rol {Rol}", model.UserName, model.Role);
        TempData["Mensaje"] = result.Message ?? "El usuario fue creado correctamente. Se envió el correo de activación.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- Editar ----------------

    [HttpGet]
    public async Task<IActionResult> Editar(string id)
    {
        if (id == UsuarioActualId)
        {
            TempData["Error"] = Mensajes.NoEditarPropiaCuenta;
            return RedirectToAction(nameof(Index));
        }

        var usuario = await userService.GetByIdAsync(id);
        if (usuario is null || usuario.Role == Roles.Comercio)
        {
            TempData["Error"] = Mensajes.UsuarioSeleccionadoNoExiste;
            return RedirectToAction(nameof(Index));
        }

        return View(mapper.Map<EditarUsuarioViewModel>(usuario));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EditarUsuarioViewModel model)
    {
        if (model.Id == UsuarioActualId)
        {
            TempData["Error"] = Mensajes.NoEditarPropiaCuenta;
            return RedirectToAction(nameof(Index));
        }

        // El rol se resuelve SIEMPRE en el servidor: el valor posteado no decide nada.
        var usuarioActual = await userService.GetByIdAsync(model.Id);
        if (usuarioActual is null || usuarioActual.Role == Roles.Comercio)
        {
            TempData["Error"] = Mensajes.UsuarioSeleccionadoNoExiste;
            return RedirectToAction(nameof(Index));
        }

        model.Role = usuarioActual.Role;
        if (usuarioActual.Role != Roles.Cliente)
            model.AdditionalAmount = null;

        if (!ModelState.IsValid)
            return View(model);

        var request = mapper.Map<UpdateUserRequest>(model);
        var result = await userService.UpdateUserAsync(request, UsuarioActualId);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            return View(model);
        }

        logger.LogInformation("Usuario {Usuario} actualizado por el administrador", model.UserName);
        TempData["Mensaje"] = "El usuario fue actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- Activar / Inactivar ----------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(string id, bool activar)
    {
        var result = await userService.SetUserStatusAsync(id, activar, UsuarioActualId);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
        }
        else
        {
            logger.LogInformation("Cambio de estado de usuario {UsuarioId}: activo={Activo}", id, activar);
            TempData["Mensaje"] = activar
                ? "El usuario fue activado correctamente."
                : "El usuario fue inactivado correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }
}
