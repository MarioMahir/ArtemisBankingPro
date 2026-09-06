using System.Text;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Infrastructure.Identity.Contexts;
using ArtemisBankingPro.Infrastructure.Identity.Entities;
using ArtemisBankingPro.Infrastructure.Identity.Services;
using ArtemisBankingPro.Infrastructure.Identity.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ArtemisBankingPro.Infrastructure.Identity;

public static class ServiceRegistration
{
    /// <summary>Base común: contexto, Identity core y servicios de cuenta.</summary>
    private static IServiceCollection AddIdentityCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(IdentityContext).Assembly.FullName)));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<IdentityContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAccountService, AccountService>();

        // IJwtService se registra en el núcleo: los handlers CQRS de Application
        // (compartidos por ambos hosts) lo requieren para construirse.
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddScoped<IJwtService, JwtService>();

        return services;
    }

    /// <summary>WebApp MVC: autenticación por cookies con las rutas exigidas por el spec.</summary>
    public static IServiceCollection AddIdentityLayerForWebApp(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityCore(configuration);

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccesoDenegado";
            options.ExpireTimeSpan = TimeSpan.FromHours(2);
            options.SlidingExpiration = true;
        });

        return services;
    }

    /// <summary>WebApi: autenticación JWT Bearer con respuestas 401/403 en Problem Details.</summary>
    public static IServiceCollection AddIdentityLayerForWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityCore(configuration);

        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
                };

                options.Events = new JwtBearerEvents
                {
                    // 401 y 403 con los mensajes exactos del spec, en formato Problem Details.
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/problem+json";
                        return context.Response.WriteAsync(
                            $$"""{"type":"https://tools.ietf.org/html/rfc7235#section-3.1","title":"Unauthorized","status":401,"detail":"{{Mensajes.ApiNoAutorizado}}"}""");
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/problem+json";
                        return context.Response.WriteAsync(
                            $$"""{"type":"https://tools.ietf.org/html/rfc7231#section-6.5.3","title":"Forbidden","status":403,"detail":"{{Mensajes.ApiAccesoDenegado}}"}""");
                    }
                };
            });

        return services;
    }
}
