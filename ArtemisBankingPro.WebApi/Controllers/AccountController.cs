using ArtemisBankingPro.Core.Application.Features.Account.Commands.ConfirmAccount;
using ArtemisBankingPro.Core.Application.Features.Account.Commands.GetResetToken;
using ArtemisBankingPro.Core.Application.Features.Account.Commands.Login;
using ArtemisBankingPro.Core.Application.Features.Account.Commands.ResetPassword;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApi.Controllers;

/// <summary>Autenticación y ciclo de vida de la cuenta (endpoints públicos, sin prefijo /api).</summary>
[ApiController]
[Route("account")]
[AllowAnonymous]
[Produces("application/json")]
public class AccountController(IMediator mediator) : ControllerBase
{
    /// <summary>Inicia sesión (roles de la API: Administrador y Comercio) y devuelve el JWT.</summary>
    /// <response code="200">Autenticación correcta; devuelve el token.</response>
    /// <response code="400">Datos estructuralmente inválidos (usuario/contraseña requeridos).</response>
    /// <response code="401">Credenciales inválidas o cuenta inactiva.</response>
    /// <response code="403">El usuario no tiene un rol permitido en la API.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await mediator.Send(command);
        if (result.Succeeded)
            return Ok(result.Data);

        return result.Message switch
        {
            Mensajes.ApiAccesoDenegado =>
                ServiceResultExtensions.ApiProblem(StatusCodes.Status403Forbidden, result.Message),
            Mensajes.CredencialesInvalidas or Mensajes.ApiCuentaInactiva =>
                ServiceResultExtensions.ApiProblem(StatusCodes.Status401Unauthorized, result.Message!),
            _ => ServiceResultExtensions.ApiProblem(StatusCodes.Status400BadRequest, result.Message!)
        };
    }

    /// <summary>Activa la cuenta con el token recibido en el correo (un solo uso).</summary>
    /// <response code="204">Cuenta activada correctamente.</response>
    /// <response code="400">Token inválido o ya utilizado.</response>
    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Confirm([FromBody] ConfirmAccountCommand command)
    {
        var result = await mediator.Send(command);
        return result.Succeeded
            ? NoContent()
            : ServiceResultExtensions.ApiProblem(StatusCodes.Status400BadRequest, result.Message!);
    }

    /// <summary>
    /// Solicita el restablecimiento de contraseña: desactiva temporalmente la cuenta y envía
    /// el token EN EL CUERPO del correo.
    /// </summary>
    /// <response code="204">Solicitud procesada; correo enviado.</response>
    /// <response code="400">Usuario inexistente, sin correo o sin rol permitido.</response>
    [HttpPost("get-reset-token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetResetToken([FromBody] GetResetTokenCommand command)
    {
        var result = await mediator.Send(command);
        return result.Succeeded
            ? NoContent()
            : ServiceResultExtensions.ApiProblem(StatusCodes.Status400BadRequest, result.Message!);
    }

    /// <summary>Restablece la contraseña con el token (vigencia 30 min, un solo uso) y reactiva la cuenta.</summary>
    /// <response code="204">Contraseña restablecida y cuenta reactivada.</response>
    /// <response code="400">Token inválido/expirado/usado o contraseñas que no coinciden.</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await mediator.Send(command);
        return result.Succeeded
            ? NoContent()
            : ServiceResultExtensions.ApiProblem(StatusCodes.Status400BadRequest, result.Message!);
    }
}
