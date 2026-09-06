using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Infrastructure.Identity.Contexts;
using ArtemisBankingPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBankingPro.Infrastructure.Identity.Seeds;

/// <summary>
/// Seeding obligatorio del spec: los 4 roles y un usuario por defecto de cada rol,
/// todos ACTIVOS (a diferencia de los usuarios creados por el sistema, que nacen inactivos).
/// </summary>
public static class IdentitySeed
{
    public const string DefaultPassword = "Artemis123$";

    public static async Task SeedIdentityAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await CreateDefaultUserAsync(userManager, Roles.Administrador, "admin", "Ana", "Martínez", "00100000001", "admin@artemisbank.com");
        await CreateDefaultUserAsync(userManager, Roles.Cajero, "cajero", "Carlos", "Pérez", "00100000002", "cajero@artemisbank.com");
        await CreateDefaultUserAsync(userManager, Roles.Cliente, "cliente", "Laura", "Gómez", "00100000003", "cliente@artemisbank.com");
        await CreateDefaultUserAsync(userManager, Roles.Comercio, "comercio", "Hermes", "Store", "00100000004", "comercio@artemisbank.com");
    }

    private static async Task CreateDefaultUserAsync(
        UserManager<ApplicationUser> userManager,
        string role, string userName, string firstName, string lastName, string identification, string email)
    {
        if (await userManager.FindByNameAsync(userName) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            Identification = identification,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, DefaultPassword);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }
}
