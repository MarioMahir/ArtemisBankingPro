using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Mappings;
using ArtemisBankingPro.Core.Application.Services;
using AutoMapper;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Infrastructure.Persistence.Repositories;
using ArtemisBankingPro.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ArtemisBankingPro.Tests.Integration.Flows;

/// <summary>
/// Flujo completo con SavingsAccountService REAL sobre SQLite en memoria:
/// cancelar una cuenta secundaria con balance transfiere los fondos a la principal
/// registrando el DÉBITO y el CRÉDITO cruzados dentro de una transacción de BD.
/// </summary>
public class CancelSecondaryAccountFlowTests : SqliteTestBase
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<IProductNumberGenerator> _numberGenerator = new();

    private SavingsAccountService CreateService()
    {
        _accountService.Setup(a => a.GetByIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync([]);
        return new SavingsAccountService(
            new GenericRepository<SavingsAccount>(Context),
            new GenericRepository<Transaction>(Context),
            _numberGenerator.Object,
            _accountService.Object,
            new UnitOfWork(Context),
            new MapperConfiguration(cfg => cfg.AddProfile<EntityMappingProfile>()).CreateMapper());
    }

    [Fact]
    public async Task CancelarSecundariaConBalance_TransfiereYRegistraDebitoYCreditoCruzados()
    {
        // Arrange: principal con 1,000 y secundaria con 350.
        Context.SavingsAccounts.Add(NuevaCuenta("111111111", "cli-1", AccountType.Principal, 1_000m));
        Context.SavingsAccounts.Add(NuevaCuenta("222222222", "cli-1", AccountType.Secundaria, 350m));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        var service = CreateService();

        // Act
        var resultado = await service.CancelAsync("222222222");

        // Assert
        Assert.True(resultado.Succeeded);

        using var verificacion = CreateContext();

        var principal = await verificacion.SavingsAccounts.SingleAsync(a => a.AccountNumber == "111111111");
        var secundaria = await verificacion.SavingsAccounts.SingleAsync(a => a.AccountNumber == "222222222");
        Assert.Equal(1_350m, principal.Balance);                 // recibió los fondos
        Assert.Equal(0m, secundaria.Balance);                    // quedó en cero
        Assert.Equal(ProductStatus.Cancelada, secundaria.Status);
        Assert.Equal(ProductStatus.Activa, principal.Status);    // la principal sigue activa

        // DÉBITO en la secundaria + CRÉDITO en la principal, ambos APROBADOS.
        var transacciones = await verificacion.Transactions.ToListAsync();
        Assert.Equal(2, transacciones.Count);

        var debito = transacciones.Single(t => t.Type == TransactionType.Debito);
        Assert.Equal(secundaria.Id, debito.AccountId);
        Assert.Equal(350m, debito.Amount);
        Assert.Equal("222222222", debito.Origin);
        Assert.Equal("111111111", debito.Beneficiary);
        Assert.Equal(TransactionStatus.Aprobada, debito.Status);

        var credito = transacciones.Single(t => t.Type == TransactionType.Credito);
        Assert.Equal(principal.Id, credito.AccountId);
        Assert.Equal(350m, credito.Amount);
        Assert.Equal("222222222", credito.Origin);
        Assert.Equal("111111111", credito.Beneficiary);
        Assert.Equal(TransactionStatus.Aprobada, credito.Status);
    }

    [Fact]
    public async Task CancelarSecundariaSinBalance_CancelaSinMovimientos()
    {
        // Arrange
        Context.SavingsAccounts.Add(NuevaCuenta("111111111", "cli-1", AccountType.Principal, 1_000m));
        Context.SavingsAccounts.Add(NuevaCuenta("222222222", "cli-1", AccountType.Secundaria, 0m));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        var service = CreateService();

        // Act
        var resultado = await service.CancelAsync("222222222");

        // Assert
        Assert.True(resultado.Succeeded);
        using var verificacion = CreateContext();
        Assert.Empty(await verificacion.Transactions.ToListAsync());
        Assert.Equal(ProductStatus.Cancelada,
            (await verificacion.SavingsAccounts.SingleAsync(a => a.AccountNumber == "222222222")).Status);
    }

    [Fact]
    public async Task CancelarPrincipal_EsRechazadoYNadaCambia()
    {
        // Arrange
        Context.SavingsAccounts.Add(NuevaCuenta("111111111", "cli-1", AccountType.Principal, 1_000m));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        var service = CreateService();

        // Act
        var resultado = await service.CancelAsync("111111111");

        // Assert: las principales JAMÁS se cancelan.
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.PrincipalNoCancelable, resultado.Message);

        using var verificacion = CreateContext();
        var principal = await verificacion.SavingsAccounts.SingleAsync();
        Assert.Equal(ProductStatus.Activa, principal.Status);
        Assert.Equal(1_000m, principal.Balance);
    }

    [Fact]
    public async Task CrearSecundariaConBalanceInicial_PersisteCuentaYCredito()
    {
        // Arrange: cliente activo con principal activa.
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(
            new Core.Application.Dtos.Users.UserDto
            {
                Id = "cli-1", FirstName = "Ana", LastName = "Pérez", Identification = "001",
                Email = "ana@test.com", UserName = "ana", Role = Roles.Cliente, IsActive = true
            });
        _numberGenerator.Setup(g => g.GenerateAccountOrLoanNumberAsync()).ReturnsAsync("333333333");
        Context.SavingsAccounts.Add(NuevaCuenta("111111111", "cli-1", AccountType.Principal, 1_000m));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        var service = CreateService();

        // Act
        var resultado = await service.CreateSecondaryAsync("cli-1", 400m, "admin-1");

        // Assert
        Assert.True(resultado.Succeeded);
        using var verificacion = CreateContext();
        var secundaria = await verificacion.SavingsAccounts.SingleAsync(a => a.AccountNumber == "333333333");
        Assert.Equal(AccountType.Secundaria, secundaria.Type);
        Assert.Equal(400m, secundaria.Balance);
        Assert.Equal("admin-1", secundaria.AdminUserId);

        var credito = await verificacion.Transactions.SingleAsync();
        Assert.Equal(TransactionType.Credito, credito.Type);
        Assert.Equal(400m, credito.Amount);
        Assert.Equal(secundaria.Id, credito.AccountId);
    }
}
