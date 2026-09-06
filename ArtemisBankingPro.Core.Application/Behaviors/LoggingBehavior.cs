using System.Diagnostics;
using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Features.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ArtemisBankingPro.Core.Application.Behaviors;

/// <summary>
/// Registra cada Command/Query: nombre del request, usuario actuante (si el request
/// lo expone vía IActingUserRequest; el resto del contexto de usuario/rol/endpoint
/// lo aporta el request logging de Serilog en la API), resultado y duración.
/// NUNCA loguea el contenido del request (puede incluir contraseñas, tokens o CVC).
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var actingUser = (request as IActingUserRequest)?.ActingUserId ?? "n/d";
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Ejecutando {RequestName} (usuario: {ActingUserId})", requestName, actingUser);

        try
        {
            var response = await next();
            stopwatch.Stop();

            if (response is ServiceResult { Succeeded: false } failure)
            {
                logger.LogWarning(
                    "{RequestName} rechazado por regla de negocio en {ElapsedMs} ms (usuario: {ActingUserId}): {Message}",
                    requestName, stopwatch.ElapsedMilliseconds, actingUser, failure.Message);
            }
            else
            {
                logger.LogInformation(
                    "{RequestName} completado correctamente en {ElapsedMs} ms (usuario: {ActingUserId})",
                    requestName, stopwatch.ElapsedMilliseconds, actingUser);
            }

            return response;
        }
        catch (Exceptions.ValidationException ex)
        {
            stopwatch.Stop();
            logger.LogWarning(
                "{RequestName} falló la validación estructural en {ElapsedMs} ms (usuario: {ActingUserId}): {ErrorCount} error(es)",
                requestName, stopwatch.ElapsedMilliseconds, actingUser, ex.Errors.Count);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "{RequestName} lanzó una excepción no controlada tras {ElapsedMs} ms (usuario: {ActingUserId})",
                requestName, stopwatch.ElapsedMilliseconds, actingUser);
            throw;
        }
    }
}
