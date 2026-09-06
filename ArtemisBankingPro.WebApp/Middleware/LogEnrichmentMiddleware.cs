using System.Security.Claims;
using Serilog.Context;

namespace ArtemisBankingPro.WebApp.Middleware;

/// <summary>
/// Enriquece todos los logs de la petición con id de correlación, usuario y rol.
/// Nunca añade contraseñas, tokens ni números completos de tarjeta.
/// </summary>
public class LogEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.TraceIdentifier;
        var userName = context.User.Identity?.IsAuthenticated == true
            ? context.User.Identity!.Name ?? "desconocido"
            : "anónimo";
        var role = context.User.FindFirstValue(ClaimTypes.Role) ?? "-";

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserName", userName))
        using (LogContext.PushProperty("Role", role))
        {
            await next(context);
        }
    }
}
