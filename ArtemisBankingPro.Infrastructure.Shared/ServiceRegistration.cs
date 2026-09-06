using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Infrastructure.Shared.Services;
using ArtemisBankingPro.Infrastructure.Shared.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBankingPro.Infrastructure.Shared;

public static class ServiceRegistration
{
    public static IServiceCollection AddSharedLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        services.AddTransient<IEmailService, EmailService>();
        services.AddSingleton<IHashingService, HashingService>();
        services.AddScoped<IProductNumberGenerator, ProductNumberGenerator>();

        return services;
    }
}
