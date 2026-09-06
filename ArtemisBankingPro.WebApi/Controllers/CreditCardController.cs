using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Features.CreditCards.Commands.CancelCreditCard;
using ArtemisBankingPro.Core.Application.Features.CreditCards.Commands.CreateCreditCard;
using ArtemisBankingPro.Core.Application.Features.CreditCards.Commands.UpdateCardLimit;
using ArtemisBankingPro.Core.Application.Features.CreditCards.Queries.GetCreditCardById;
using ArtemisBankingPro.Core.Application.Features.CreditCards.Queries.GetCreditCards;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApi.Controllers;

/// <summary>Gestión de tarjetas de crédito (solo Administrador). Números SIEMPRE enmascarados.</summary>
[ApiController]
[Route("api/credit-card")]
[Authorize(Roles = Roles.Administrador)]
[Produces("application/json")]
public class CreditCardController(IMediator mediator) : ControllerBase
{
    private string ActingUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Listado paginado (20) con maskedCardNumber (***********1234). status: activas (default) | canceladas | todas.</summary>
    /// <response code="200">Página de tarjetas.</response>
    /// <response code="404">No existe un cliente con la cédula indicada.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CreditCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] string? status = null, [FromQuery] string? identification = null)
    {
        var result = await mediator.Send(new GetCreditCardsQuery
        {
            Page = page,
            Status = status,
            Identification = identification
        });
        return result.Succeeded ? Ok(result.Data) : result.ToErrorResult();
    }

    /// <summary>Detalle de la tarjeta con sus consumos (consumptions[]), más recientes primero.</summary>
    /// <response code="200">Tarjeta encontrada.</response>
    /// <response code="404">La tarjeta no existe.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CreditCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await mediator.Send(new GetCreditCardByIdQuery { Id = id });
        return result.Succeeded ? Ok(result.Data) : result.ToErrorResult();
    }

    /// <summary>Asigna una tarjeta: deuda inicial RD$0.00, expiración hoy + 3 años, CVC solo hash.</summary>
    /// <response code="201">Tarjeta creada; Location apunta al detalle.</response>
    /// <response code="400">Límite inválido o cliente no activo.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreditCardDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateCreditCardCommand command)
    {
        command.ActingUserId = ActingUserId;
        var result = await mediator.Send(command);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : result.ToErrorResult();
    }

    /// <summary>Nuevo límite: tarjeta activa, mayor que cero y no inferior a la deuda actual.</summary>
    /// <response code="204">Límite actualizado.</response>
    /// <response code="400">Límite inválido, inferior a la deuda o tarjeta cancelada.</response>
    /// <response code="404">La tarjeta no existe.</response>
    [HttpPatch("{id:int}/limit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLimit(int id, [FromBody] UpdateCardLimitCommand command)
    {
        command.CardId = id;
        var result = await mediator.Send(command);
        return result.Succeeded ? NoContent() : result.ToErrorResult();
    }

    /// <summary>Cancela la tarjeta SOLO si la deuda es RD$0.00 (el historial se conserva).</summary>
    /// <response code="204">Tarjeta cancelada.</response>
    /// <response code="400">La tarjeta tiene deuda pendiente (mensaje exacto del spec).</response>
    /// <response code="404">La tarjeta no existe.</response>
    [HttpPatch("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await mediator.Send(new CancelCreditCardCommand { CardId = id });
        return result.Succeeded ? NoContent() : result.ToErrorResult();
    }
}
