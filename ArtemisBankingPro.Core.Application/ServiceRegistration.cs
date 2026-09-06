using ArtemisBankingPro.Core.Application.Behaviors;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBankingPro.Core.Application;

public static class ServiceRegistration
{
    /// <summary>
    /// Servicios de negocio de la capa Application + CQRS de la API:
    /// MediatR (Features), FluentValidation (validación estructural), Behaviors
    /// (Logging → Validation, en ese orden) y AutoMapper (WebApiMappingProfile).
    /// </summary>
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        var assembly = typeof(ServiceRegistration).Assembly;

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            // Orden del pipeline: 1º Logging (mide y registra todo), 2º Validation
            // (lanza ValidationException si falla la validación estructural).
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddAutoMapper(_ => { }, assembly);

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISavingsAccountService, SavingsAccountService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<ICreditCardService, CreditCardService>();
        services.AddScoped<IBeneficiaryService, BeneficiaryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ICashAdvanceService, CashAdvanceService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICommerceService, CommerceService>();
        services.AddScoped<IHermesPayService, HermesPayService>();

        return services;
    }
}
