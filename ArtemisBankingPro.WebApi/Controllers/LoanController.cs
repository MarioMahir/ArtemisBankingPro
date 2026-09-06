using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Features.Loans.Commands.CreateLoan;
using ArtemisBankingPro.Core.Application.Features.Loans.Commands.UpdateLoanRate;
using ArtemisBankingPro.Core.Application.Features.Loans.Queries.GetLoanById;
using ArtemisBankingPro.Core.Application.Features.Loans.Queries.GetLoans;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApi.Controllers;

/// <summary>Gestión de préstamos (solo Administrador).</summary>
[ApiController]
[Route("api/loan")]
[Authorize(Roles = Roles.Administrador)]
[Produces("application/json")]
public class LoanController(IMediator mediator) : ControllerBase
{
    private string ActingUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Listado paginado (20). status: activos (default) | completados | todos; búsqueda por cédula.</summary>
    /// <response code="200">Página de préstamos con estado del cliente (al día / en mora).</response>
    /// <response code="404">No existe un cliente con la cédula indicada.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LoanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] string? status = null, [FromQuery] string? identification = null)
    {
        var result = await mediator.Send(new GetLoansQuery
        {
            Page = page,
            Status = status,
            Identification = identification
        });
        return result.Succeeded ? Ok(result.Data) : result.ToErrorResult();
    }

    /// <summary>Detalle del préstamo con su tabla de amortización (amortization[]).</summary>
    /// <response code="200">Préstamo encontrado.</response>
    /// <response code="404">El préstamo no existe.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LoanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await mediator.Send(new GetLoanByIdQuery { Id = id });
        return result.Succeeded ? Ok(result.Data) : result.ToErrorResult();
    }

    /// <summary>
    /// Asigna un préstamo (sistema francés; desembolso a la cuenta principal). Si el cliente
    /// es o se convertirá en alto riesgo y confirmHighRisk es false → 409 con riskType
    /// (CurrentHighRisk/ProjectedHighRisk), currentDebt, projectedDebt y averageDebt.
    /// </summary>
    /// <response code="201">Préstamo creado; Location apunta al detalle.</response>
    /// <response code="400">Datos inválidos o cliente sin cuenta principal activa.</response>
    /// <response code="409">Alto riesgo sin confirmar, o el cliente ya tiene un préstamo activo.</response>
    [HttpPost]
    [ProducesResponseType(typeof(LoanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(HighRiskConflict), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateLoanCommand command)
    {
        command.ActingUserId = ActingUserId;
        var result = await mediator.Send(command);

        // Alto riesgo sin confirmar → 409 con el cuerpo exacto del spec.
        if (result.Conflict is not null)
            return Conflict(result.Conflict);

        return result.Result!.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Result.Data!.Id }, result.Result.Data)
            : result.Result.ToErrorResult();
    }

    /// <summary>Edita la tasa anual recalculando SOLO las cuotas pendientes con vencimiento futuro.</summary>
    /// <response code="204">Tasa actualizada.</response>
    /// <response code="400">Tasa negativa, préstamo no activo o sin cuotas futuras pendientes.</response>
    /// <response code="404">El préstamo no existe.</response>
    [HttpPatch("{id:int}/rate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRate(int id, [FromBody] UpdateLoanRateCommand command)
    {
        command.LoanId = id;
        var result = await mediator.Send(command);
        return result.Succeeded ? NoContent() : result.ToErrorResult();
    }
}
