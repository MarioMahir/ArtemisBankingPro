using System.Reflection;
using System.Security.Claims;
using ArtemisBankingPro.Core.Application;
using ArtemisBankingPro.Infrastructure.Identity;
using ArtemisBankingPro.Infrastructure.Identity.Seeds;
using ArtemisBankingPro.Infrastructure.Persistence;
using ArtemisBankingPro.Infrastructure.Persistence.Seeds;
using ArtemisBankingPro.Infrastructure.Shared;
using ArtemisBankingPro.WebApi.Handlers;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Context;
using Serilog.Events;

// ---------- Serilog: consola + archivo rolling diario con id de correlación ----------
const string outputTemplate =
    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: outputTemplate)
    .WriteTo.File("logs/webapi-.log", rollingInterval: RollingInterval.Day, outputTemplate: outputTemplate)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ---------- Capas (Onion): Application + Persistence + Identity(JWT) + Shared ----------
    builder.Services.AddApplicationLayer();
    builder.Services.AddPersistenceLayer(builder.Configuration);
    builder.Services.AddIdentityLayerForWebApi(builder.Configuration);
    builder.Services.AddSharedLayer(builder.Configuration);

    // La validación estructural de los Commands/Queries es responsabilidad de
    // FluentValidation (ValidationBehavior); se desactiva el [Required] implícito
    // que MVC aplica a las propiedades string no anulables al hacer binding.
    builder.Services.AddControllers(options =>
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

    // ---------- Global Exception Handler + Problem Details (RFC 7807) ----------
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ---------- Swagger con seguridad Bearer JWT ----------
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Artemis Banking Pro API",
            Version = "v1",
            Description = "Web API de Artemis Banking Pro: cuentas, usuarios, préstamos, tarjetas, " +
                          "cuentas de ahorro, comercios y el procesador de pagos Hermes Pay. " +
                          "Autenticación JWT (roles Administrador y Comercio)."
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath);

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Introduzca el token JWT obtenido en POST /account/login."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    app.UseExceptionHandler();

    // Id de correlación: cabecera X-Correlation-Id o el TraceIdentifier de la petición.
    app.Use(async (context, next) =>
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? context.TraceIdentifier;
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next();
        }
    });

    app.UseSerilogRequestLogging(options =>
    {
        // Fecha/nivel/duración los pone Serilog; aquí se agregan usuario, rol y endpoint.
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("Usuario", httpContext.User.Identity?.Name ?? "anónimo");
            diagnosticContext.Set("Rol", httpContext.User.FindFirst(ClaimTypes.Role)?.Value ?? "n/d");
            diagnosticContext.Set("Endpoint", $"{httpContext.Request.Method} {httpContext.Request.Path}");
        };
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} → {StatusCode} en {Elapsed:0.0} ms (usuario: {Usuario}, rol: {Rol})";
    });

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Artemis Banking Pro API v1");
    });

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // ---------- Seeds: roles + usuarios por defecto (Activos) y datos de la app ----------
    await app.Services.SeedIdentityAsync();
    await app.Services.SeedAppAsync();

    Log.Information("Artemis Banking Pro Web API iniciada");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La Web API terminó inesperadamente");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
