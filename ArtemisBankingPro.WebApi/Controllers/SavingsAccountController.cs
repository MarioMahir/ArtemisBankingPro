using System.Security.Claims;
using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Application.Dtos.Transactions;
using ArtemisBankingPro.Core.Application.Features.SavingsAccounts.Commands.CancelSavingsAccount;
using ArtemisBankingPro.Core.Application.Features.SavingsAccounts.Commands.CreateSavingsAccount;
using ArtemisBankingPro.Core.Application.Features.SavingsAccounts.Queries.GetAccountTransactions;
using ArtemisBankingPro.Core.Application.Features.SavingsAccounts.Queries.GetSavingsAccounts;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApi.Controllers;

/// <summary>Gestión de cuentas de ahorro (solo Administrador).</summary>
[ApiController]
[Route("api/savings-account")]
[Authorize(Roles = Roles.Administrador)]
[Produces("application/json")]
public class SavingsAccountController(IMediator mediator) : ControllerBase
{
    private string ActingUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Listado paginado (20). status: activa (default) | cancelada | todas;
    /// type: principal | secundaria | todas (default); búsqueda por cédula.
    /// </summary>
    /// <response code="200">Página de cuentas.</response>
    /// <response code="404">No existe un cliente con la cédula indicada.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SavingsAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] string? identification = null)
    {
        var result = await mediator.Send(new GetSavingsAccountsQuery
        {
            Page = page,
            Status = status,
            Type = type,
            Identification = identification
        });
        return result.Succeeded ? Ok(result.Data) : result.ToErrorResult();
    }

    /// <summary>Asigna una cuenta SECUNDARIA a un cliente activo con principal activa (balance inicial ≥ 0).</summary>
    /// <response code="201">Cuenta creada; Location apunta a sus transacciones.</response>
    /// <response code="400">Balance inválido, cliente inactivo o sin principal activa.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SavingsAccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateSavingsAccountCommand command)
    {
        command.ActingUserId = ActingUserId;
        var result = await mediator.Send(command);
        return result.Succeeded
            ? CreatedAtAction(
                nameof(GetTransactions),
                new { accountNumber = result.Data!.AccountNumber },
                result.Data)
            : result.ToErrorResult();
    }

    /// <summary>Transacciones de la cuenta, paginadas (20), más recientes primero.</summary>
    /// <response code="200">Página de transacciones.</response>
    /// <response code="404">La cuenta no existe.</response>
    [HttpGet("{accountNumber}/transactions")]
    [ProducesResponseType(typeof(PagedResult<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactions(string accountNumber, [FromQuery] int page = 1)
    {
        var result = await mediator.Send(new GetAccountTransactionsQuery
        {
            AccountNumber = accountNumber,
            Page = page
        });
        return result.Succeeded ? Ok(result.Data) : result.ToErrorResult();
    }

    /// <summary>
    /// Cancela una cuenta SECUNDARIA transfiriendo su balance a la principal.
    /// Las principales no pueden cancelarse (400 con el mensaje exacto del spec).
    /// </summary>
    /// <response code="204">Cuenta cancelada.</response>
    /// <response code="400">Cuenta principal, ya cancelada o sin principal activa para los fondos.</response>
    /// <response code="404">La cuenta no existe.</response>
    [HttpPatch("{accountNumber}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(string accountNumber)
    {
        var result = await mediator.Send(new CancelSavingsAccountCommand { AccountNumber = accountNumber });
        return result.Succeeded ? NoContent() : result.ToErrorResult();
    }
}
