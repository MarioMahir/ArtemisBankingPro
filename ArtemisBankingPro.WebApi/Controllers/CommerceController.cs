using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Commerces;
using ArtemisBankingPro.Core.Application.Features.Commerces.Commands.CreateCommerce;
using ArtemisBankingPro.Core.Application.Features.Commerces.Commands.UpdateCommerce;
using ArtemisBankingPro.Core.Application.Features.Commerces.Commands.UpdateCommerceStatus;
using ArtemisBankingPro.Core.Application.Features.Commerces.Queries.GetCommerceById;
using ArtemisBankingPro.Core.Application.Features.Commerces.Queries.GetCommerces;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApi.Controllers;

/// <summary>Gestión de comercios afiliados a Hermes Pay (solo Administrador).</summary>
[ApiController]
[Route("api/commerce")]
[Authorize(Roles = Roles.Administrador)]
[Produces("application/json")]
public class CommerceController(IMediator mediator) : ControllerBase
{
    /// <summary>Listado paginado (20). status: activos (default) | inactivos | todos. Incluye hasAssociatedUser.</summary>
    /// <response code="200">Página de comercios.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CommerceApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] string? status = null) =>
        Ok(await mediator.Send(new GetCommercesQuery { Page = page, Status = status }));

    /// <summary>Detalle del comercio con su usuario asociado (associatedUser, null si no tiene).</summary>
    /// <response code="200">Comercio encontrado.</response>
    /// <response code="404">El comercio no existe.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CommerceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await mediator.Send(new GetCommerceByIdQuery { Id = id });
        return result.Succeeded ? Ok(result.Data) : result.ToErrorResult();
    }

    /// <summary>Registra un comercio (RNC y correo únicos). Nace Activo y SIN usuario asociado.</summary>
    /// <response code="201">Comercio creado; Location apunta al detalle.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="409">RNC o correo ya registrados en otro comercio.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CommerceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCommerceCommand command)
    {
        var result = await mediator.Send(command);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : result.ToErrorResult();
    }

    /// <summary>Edita los datos del comercio SIN tocar su estado (RNC/correo no pueden ser de otro).</summary>
    /// <response code="204">Comercio actualizado.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="404">El comercio no existe.</response>
    /// <response code="409">RNC o correo pertenecen a otro comercio.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCommerceCommand command)
    {
        command.Id = id;
        var result = await mediator.Send(command);
        return result.Succeeded ? NoContent() : result.ToErrorResult();
    }

    /// <summary>Activa/desactiva el comercio. Desactivar inactiva sus usuarios; reactivar NO los reactiva.</summary>
    /// <response code="204">Estado actualizado.</response>
    /// <response code="404">El comercio no existe.</response>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateCommerceStatusCommand command)
    {
        command.Id = id;
        var result = await mediator.Send(command);
        return result.Succeeded ? NoContent() : result.ToErrorResult();
    }
}
