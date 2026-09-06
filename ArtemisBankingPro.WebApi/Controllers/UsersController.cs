using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Features.Users.Commands.CreateCommerceUser;
using ArtemisBankingPro.Core.Application.Features.Users.Commands.CreateUser;
using ArtemisBankingPro.Core.Application.Features.Users.Commands.UpdateUser;
using ArtemisBankingPro.Core.Application.Features.Users.Commands.UpdateUserStatus;
using ArtemisBankingPro.Core.Application.Features.Users.Queries.GetCommerceUsers;
using ArtemisBankingPro.Core.Application.Features.Users.Queries.GetUserById;
using ArtemisBankingPro.Core.Application.Features.Users.Queries.GetUsers;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApi.Controllers;

/// <summary>Gestión de usuarios (solo Administrador).</summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Administrador)]
[Produces("application/json")]
public class UsersController(IMediator mediator) : ControllerBase
{
    private string ActingUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Listado paginado (20) de usuarios de la WebApp (excluye Comercio), con filtro por rol.</summary>
    /// <response code="200">Página de usuarios, más recientes primero.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] string? role = null) =>
        Ok(await mediator.Send(new GetUsersQuery { Page = page, Role = role }));

    /// <summary>Listado paginado (20) de usuarios con rol Comercio.</summary>
    /// <response code="200">Página de usuarios de comercio.</response>
    [HttpGet("commerce")]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCommerceUsers([FromQuery] int page = 1) =>
        Ok(await mediator.Send(new GetCommerceUsersQuery { Page = page }));

    /// <summary>Detalle del usuario incluyendo su cuenta principal (mainAccount).</summary>
    /// <response code="200">Usuario encontrado.</response>
    /// <response code="404">No existe un usuario con ese id.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserWithMainAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await mediator.Send(new GetUserByIdQuery { Id = id });
        return result.Succeeded ? Ok(result.Data) : result.ToErrorResult();
    }

    /// <summary>
    /// Crea un usuario Administrador, Cajero o Cliente (nace Inactivo; token de activación en el
    /// cuerpo del correo). Cliente → cuenta de ahorro principal automática.
    /// </summary>
    /// <response code="201">Usuario creado; Location apunta al detalle.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="409">Cédula, correo o nombre de usuario ya registrados.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
    {
        var result = await mediator.Send(command);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : result.ToErrorResult();
    }

    /// <summary>
    /// Crea el usuario (rol Comercio) del comercio indicado. initialAmount es REQUERIDO;
    /// el comercio debe existir y no tener otro usuario; cuenta principal automática.
    /// </summary>
    /// <response code="201">Usuario de comercio creado.</response>
    /// <response code="400">Datos inválidos (initialAmount requerido, formatos).</response>
    /// <response code="404">El comercio no existe.</response>
    /// <response code="409">El comercio ya tiene un usuario asociado o datos duplicados.</response>
    [HttpPost("commerce/{commerceId:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCommerceUser(int commerceId, [FromBody] CreateCommerceUserCommand command)
    {
        command.CommerceId = commerceId;
        var result = await mediator.Send(command);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : result.ToErrorResult();
    }

    /// <summary>
    /// Edita datos del usuario (sin cambiar el rol). additionalAmount ≥ 0 se acredita a la
    /// cuenta principal. El administrador no puede editarse a sí mismo.
    /// </summary>
    /// <response code="204">Usuario actualizado.</response>
    /// <response code="400">Datos inválidos o intento de auto-edición.</response>
    /// <response code="404">El usuario no existe.</response>
    /// <response code="409">Cédula, correo o nombre de usuario de otro usuario.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserCommand command)
    {
        command.Id = id;
        command.ActingUserId = ActingUserId;
        var result = await mediator.Send(command);
        return result.Succeeded ? NoContent() : result.ToErrorResult();
    }

    /// <summary>Activa/inactiva al usuario. El administrador no puede modificar su propio estado.</summary>
    /// <response code="204">Estado actualizado.</response>
    /// <response code="400">Intento de modificar el propio estado.</response>
    /// <response code="404">El usuario no existe.</response>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateUserStatusCommand command)
    {
        command.UserId = id;
        command.ActingUserId = ActingUserId;
        var result = await mediator.Send(command);
        return result.Succeeded ? NoContent() : result.ToErrorResult();
    }
}
