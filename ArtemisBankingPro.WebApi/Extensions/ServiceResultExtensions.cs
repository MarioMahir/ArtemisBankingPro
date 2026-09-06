using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Features.Users.Commands.CreateCommerceUser;
using ArtemisBankingPro.Core.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.WebApi.Extensions;

/// <summary>
/// CRITERIO DE TRADUCCIÓN ServiceResult → HTTP (los éxitos los decide cada acción:
/// 200 con cuerpo, 201 con Location o 204 sin contenido):
///  - Mensajes de inexistencia del recurso ("... no existe")            → 404 Not Found.
///  - Duplicados y conflictos de estado (cédula/correo/usuario/RNC ya
///    registrados, cliente con préstamo activo, comercio con usuario)   → 409 Conflict.
///    (El alto riesgo de préstamo tiene su propio cuerpo 409 y se arma en LoanController.)
///  - Cualquier otro rechazo de negocio                                 → 400 Bad Request.
/// Todos los errores viajan como Problem Details (RFC 7807) con el mensaje exacto en "detail".
/// </summary>
public static class ServiceResultExtensions
{
    private static readonly HashSet<string> NotFoundMessages =
    [
        Mensajes.UsuarioNoExiste,
        Mensajes.PrestamoNoExiste,
        Mensajes.TarjetaNoExiste,
        Mensajes.CuentaNoExiste,
        Mensajes.ComercioNoExiste,
        Mensajes.ClienteNoExistePorCedula
    ];

    private static readonly HashSet<string> ConflictMessages =
    [
        Mensajes.CedulaDuplicada,
        Mensajes.CorreoDuplicado,
        Mensajes.UsuarioDuplicado,
        Mensajes.ComercioRncDuplicado,
        Mensajes.ComercioCorreoDuplicado,
        Mensajes.ClienteConPrestamoActivo,
        CreateCommerceUserCommand.ComercioYaTieneUsuario
    ];

    /// <summary>Traduce un ServiceResult fallido al ProblemDetails con el status correcto.</summary>
    public static ObjectResult ToErrorResult(this ServiceResult result)
    {
        var message = result.Message ?? "La solicitud no es válida.";
        var status = NotFoundMessages.Contains(message) ? StatusCodes.Status404NotFound
            : ConflictMessages.Contains(message) ? StatusCodes.Status409Conflict
            : StatusCodes.Status400BadRequest;

        return ApiProblem(status, message);
    }

    /// <summary>ProblemDetails (RFC 7807) con el título estándar del status.</summary>
    public static ObjectResult ApiProblem(int statusCode, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                StatusCodes.Status400BadRequest => "Bad Request",
                StatusCodes.Status401Unauthorized => "Unauthorized",
                StatusCodes.Status403Forbidden => "Forbidden",
                StatusCodes.Status404NotFound => "Not Found",
                StatusCodes.Status409Conflict => "Conflict",
                _ => "Error"
            },
            Detail = detail
        };

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }
}
