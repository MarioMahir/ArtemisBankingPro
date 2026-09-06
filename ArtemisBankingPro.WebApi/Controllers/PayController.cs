using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Features.Payments.Commands.ProcessPayment;
using ArtemisBankingPro.Core.Application.Features.Payments.Queries.GetCommerceTransactions;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApi.Controllers;

/// <summary>
/// Procesador de pagos Hermes Pay (Administrador o Comercio, sin prefijo /api).
/// REGLA CRÍTICA: el rol Comercio opera SIEMPRE sobre su propio comercio — el commerceId
/// efectivo sale del claim "commerceId" del JWT y se IGNORA el de la URL; el Administrador
/// usa el de la URL. Comercio sin comercio asociado → 403.
/// </summary>
[ApiController]
[Route("pay")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.Comercio}")]
[Produces("application/json")]
public class PayController(IMediator mediator) : ControllerBase
{
    /// <summary>Resuelve el commerceId efectivo según el rol; null → Comercio sin comercio asociado.</summary>
    private int? ResolveEffectiveCommerceId(int commerceIdFromRoute)
    {
        if (!User.IsInRole(Roles.Comercio))
            return commerceIdFromRoute; // Administrador → el de la URL

        var claim = User.FindFirst("commerceId")?.Value;
        return int.TryParse(claim, out var commerceId) ? commerceId : null;
    }

    /// <summary>Consumos (APROBADOS/RECHAZADOS) del comercio, paginados (20), más recientes primero.</summary>
    /// <response code="200">Página de transacciones del comercio.</response>
    /// <response code="403">Usuario Comercio sin comercio asociado o rol no permitido.</response>
    /// <response code="404">El comercio no existe.</response>
    [HttpGet("get-transactions/{commerceId:int}")]
    [ProducesResponseType(typeof(PagedResult<ConsumptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactions(int commerceId, [FromQuery] int page = 1)
    {
        var effectiveCommerceId = ResolveEffectiveCommerceId(commerceId);
        if (effectiveCommerceId is null)
            return ServiceResultExtensions.ApiProblem(StatusCodes.Status403Forbidden, Mensajes.ApiAccesoDenegado);

        var result = await mediator.Send(new GetCommerceTransactionsQuery
        {
            CommerceId = effectiveCommerceId.Value,
            Page = page
        });
        return result.Succeeded ? Ok(result.Data) : result.ToErrorResult();
    }

    /// <summary>
    /// Procesa un pago con tarjeta contra el comercio (transaccional): valida tarjeta, CVC,
    /// vigencia, comercio y crédito disponible. Aprobado → deuda sube + consumo APROBADO +
    /// CRÉDITO a la cuenta principal del comercio.
    /// </summary>
    /// <response code="204">Pago procesado correctamente.</response>
    /// <response code="400">Rechazo (crédito insuficiente, tarjeta/comercio inválidos) o datos con formato incorrecto.</response>
    /// <response code="403">Usuario Comercio sin comercio asociado o rol no permitido.</response>
    [HttpPost("process-payment/{commerceId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ProcessPayment(int commerceId, [FromBody] ProcessPaymentCommand command)
    {
        var effectiveCommerceId = ResolveEffectiveCommerceId(commerceId);
        if (effectiveCommerceId is null)
            return ServiceResultExtensions.ApiProblem(StatusCodes.Status403Forbidden, Mensajes.ApiAccesoDenegado);

        command.CommerceId = effectiveCommerceId.Value;
        var result = await mediator.Send(command);

        // Rechazo por crédito → 400 con "El monto de la transacción excede el crédito disponible de la tarjeta."
        return result.Succeeded
            ? NoContent()
            : ServiceResultExtensions.ApiProblem(StatusCodes.Status400BadRequest, result.Message!);
    }
}
