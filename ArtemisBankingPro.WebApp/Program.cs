using ArtemisBankingPro.Core.Application;
using ArtemisBankingPro.Infrastructure.Identity;
using ArtemisBankingPro.Infrastructure.Identity.Seeds;
using ArtemisBankingPro.Infrastructure.Persistence;
using ArtemisBankingPro.Infrastructure.Persistence.Seeds;
using ArtemisBankingPro.Infrastructure.Shared;
using ArtemisBankingPro.WebApp.Mappings;
using ArtemisBankingPro.WebApp.Middleware;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Serilog: consola + archivo con rotación diaria, enriquecido con usuario/rol/correlación.
// PROHIBIDO loguear secretos (contraseñas, tokens, CVC, números completos de tarjeta).
builder.Host.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ArtemisBankingPro.WebApp")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} " +
                        "(usuario={UserName} rol={Role} correlacion={CorrelationId}){NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/webapp-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] " +
                        "usuario={UserName} rol={Role} correlacion={CorrelationId} {Message:lj}{NewLine}{Exception}"));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAutoMapper(_ => { }, typeof(WebAppMappingProfile).Assembly);

builder.Services.AddPersistenceLayer(builder.Configuration);
builder.Services.AddIdentityLayerForWebApp(builder.Configuration);
builder.Services.AddSharedLayer(builder.Configuration);
builder.Services.AddApplicationLayer();

var app = builder.Build();

// Global exception handler: página de error amigable (estilo Problem Details) en /Home/Error.
app.UseExceptionHandler("/Home/Error");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Enriquecimiento de logs con usuario/rol/id de correlación (requiere el usuario autenticado).
app.UseMiddleware<LogEnrichmentMiddleware>();
app.UseSerilogRequestLogging();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Seeds: roles/usuarios por defecto (Identity) y productos bancarios seed (App).
await app.Services.SeedIdentityAsync();
await app.Services.SeedAppAsync();

app.Run();
