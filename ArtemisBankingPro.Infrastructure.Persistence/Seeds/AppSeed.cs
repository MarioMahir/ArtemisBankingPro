using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBankingPro.Infrastructure.Persistence.Seeds;

/// <summary>
/// Seeding de datos bancarios: migra el AppDbContext y crea los productos de los
/// usuarios seed (cuenta principal del cliente con RD$25,000.00 y comercio demo
/// con su usuario asociado y cuenta principal en RD$0.00).
/// </summary>
public static class AppSeed
{
    public static async Task SeedAppAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var numberGenerator = scope.ServiceProvider.GetRequiredService<IProductNumberGenerator>();

        await context.Database.MigrateAsync();

        // ---- Cliente seed: cuenta principal con RD$25,000.00 ----
        var client = await accountService.GetByUserNameAsync("cliente");
        if (client is not null)
            await EnsurePrincipalAccountAsync(context, numberGenerator, client.Id, 25_000m);

        // ---- Comercio demo + usuario comercio seed ----
        var commerce = await context.Commerces.FirstOrDefaultAsync(c => c.Rnc == "130000001");
        if (commerce is null)
        {
            commerce = new Commerce
            {
                Name = "Comercio Demo",
                Rnc = "130000001",
                Email = "comercio@artemisbank.com",
                PhoneNumber = "809-555-0001",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            context.Commerces.Add(commerce);
            await context.SaveChangesAsync();
        }

        var commerceUser = await accountService.GetByUserNameAsync("comercio");
        if (commerceUser is not null)
        {
            if (commerceUser.CommerceId != commerce.Id)
                await accountService.AssignCommerceAsync(commerceUser.Id, commerce.Id);

            await EnsurePrincipalAccountAsync(context, numberGenerator, commerceUser.Id, 0m);
        }
    }

    private static async Task EnsurePrincipalAccountAsync(
        AppDbContext context, IProductNumberGenerator numberGenerator, string userId, decimal initialBalance)
    {
        var hasPrincipal = await context.SavingsAccounts
            .AnyAsync(a => a.UserId == userId && a.Type == AccountType.Principal);
        if (hasPrincipal)
            return;

        var accountNumber = await numberGenerator.GenerateAccountOrLoanNumberAsync();
        var account = new SavingsAccount
        {
            AccountNumber = accountNumber,
            Balance = initialBalance,
            Type = AccountType.Principal,
            Status = ProductStatus.Activa,
            UserId = userId,
            CreatedAt = DateTime.Now
        };
        context.SavingsAccounts.Add(account);
        await context.SaveChangesAsync();

        if (initialBalance > 0)
        {
            context.Transactions.Add(new Transaction
            {
                AccountId = account.Id,
                Amount = initialBalance,
                Type = TransactionType.Credito,
                Origin = AppConstants.OpeningOrigin,
                Beneficiary = accountNumber,
                Status = TransactionStatus.Aprobada,
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();
        }
    }
}
