using ArtemisBankingPro.Core.Application.Dtos.Email;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Dtos.Users;
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
/// Flujo completo con servicios y repositorios REALES sobre SQLite en memoria:
/// crear la cuenta principal y desembolsar un préstamo (LoanService). Solo se simulan
/// las dependencias de otras capas: IAccountService (Identity), IEmailService y el generador.
/// </summary>
public class LoanDisbursementFlowTests : SqliteTestBase
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IProductNumberGenerator> _numberGenerator = new();

    private LoanService CreateLoanService()
    {
        _numberGenerator.Setup(g => g.GenerateAccountOrLoanNumberAsync()).ReturnsAsync("900000001");
        _emailService.Setup(e => e.SendAsync(It.IsAny<EmailRequest>())).ReturnsAsync(true);
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(new UserDto
        {
            Id = "cli-1", FirstName = "Ana", LastName = "Pérez", Identification = "00112345678",
            Email = "ana@test.com", UserName = "ana", Role = Roles.Cliente, IsActive = true
        });
        _accountService.Setup(a => a.GetByIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync([]);

        return new LoanService(
            new GenericRepository<Loan>(Context),
            new GenericRepository<LoanInstallment>(Context),
            new GenericRepository<SavingsAccount>(Context),
            new GenericRepository<Transaction>(Context),
            new GenericRepository<CreditCard>(Context),
            _numberGenerator.Object,
            _accountService.Object,
            _emailService.Object,
            new UnitOfWork(Context),
            new MapperConfiguration(cfg => cfg.AddProfile<EntityMappingProfile>()).CreateMapper());
    }

    [Fact]
    public async Task CrearCuentaYDesembolsarPrestamo_BalanceFinalYTransaccionCreditoPersistidos()
    {
        // Arrange: cuenta principal activa con balance inicial de 5,000.
        Context.SavingsAccounts.Add(NuevaCuenta("111111111", "cli-1", balance: 5_000m));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        var service = CreateLoanService();

        // Act: préstamo del ejemplo del spec (100,000 a 12 meses al 12%).
        var resultado = await service.CreateLoanAsync(new CreateLoanRequest
        {
            ClientId = "cli-1", CapitalAmount = 100_000m, TermInMonths = 12, AnnualInterestRate = 12m
        }, "admin-1");

        // Assert
        Assert.True(resultado.Succeeded);

        // Verificación con un contexto NUEVO: todo quedó realmente en la base.
        using var verificacion = CreateContext();

        var cuenta = await verificacion.SavingsAccounts.SingleAsync();
        Assert.Equal(105_000m, cuenta.Balance); // 5,000 + 100,000 desembolsados

        var prestamo = await verificacion.Loans.Include(l => l.Installments).SingleAsync();
        Assert.Equal("900000001", prestamo.LoanNumber);
        Assert.Equal(LoanStatus.Activo, prestamo.Status);
        Assert.Equal("admin-1", prestamo.AdminUserId);
        Assert.Equal(12, prestamo.Installments.Count);
        Assert.All(prestamo.Installments, i => Assert.Equal(InstallmentStatus.Pendiente, i.Status));
        Assert.Equal(8_884.88m, prestamo.Installments.First(i => i.Number == 1).Value); // cuota del spec
        Assert.Equal(106_618.56m, prestamo.Installments.Sum(i => i.PendingAmount));     // total del spec

        var credito = await verificacion.Transactions.SingleAsync();
        Assert.Equal(TransactionType.Credito, credito.Type);
        Assert.Equal(100_000m, credito.Amount);
        Assert.Equal("900000001", credito.Origin);        // origen = nº del préstamo
        Assert.Equal("111111111", credito.Beneficiary);   // acreditado a la principal
        Assert.Equal(TransactionStatus.Aprobada, credito.Status);
        Assert.Equal(cuenta.Id, credito.AccountId);
    }

    [Fact]
    public async Task DesembolsarSinCuentaPrincipalActiva_NoPersisteNada()
    {
        // Arrange: la única cuenta del cliente está CANCELADA.
        Context.SavingsAccounts.Add(NuevaCuenta("111111111", "cli-1",
            balance: 5_000m, estado: ProductStatus.Cancelada));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        var service = CreateLoanService();

        // Act
        var resultado = await service.CreateLoanAsync(new CreateLoanRequest
        {
            ClientId = "cli-1", CapitalAmount = 100_000m, TermInMonths = 12, AnnualInterestRate = 12m
        }, "admin-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.ClienteSinCuentaPrincipal, resultado.Message);

        using var verificacion = CreateContext();
        Assert.Empty(await verificacion.Loans.ToListAsync());
        Assert.Empty(await verificacion.Transactions.ToListAsync());
        Assert.Equal(5_000m, (await verificacion.SavingsAccounts.SingleAsync()).Balance);
    }

    [Fact]
    public async Task SegundoPrestamoParaElMismoCliente_EsRechazado()
    {
        // Arrange: primer préstamo activo desembolsado.
        Context.SavingsAccounts.Add(NuevaCuenta("111111111", "cli-1", balance: 0m));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        var service = CreateLoanService();
        var primero = await service.CreateLoanAsync(new CreateLoanRequest
        {
            ClientId = "cli-1", CapitalAmount = 50_000m, TermInMonths = 12, AnnualInterestRate = 10m
        }, "admin-1");
        Assert.True(primero.Succeeded);
        Context.ChangeTracker.Clear(); // simula un nuevo scope de request

        // Act: regla — un cliente solo puede tener UN préstamo activo.
        var segundo = await service.CreateLoanAsync(new CreateLoanRequest
        {
            ClientId = "cli-1", CapitalAmount = 20_000m, TermInMonths = 6, AnnualInterestRate = 10m
        }, "admin-1");

        // Assert
        Assert.False(segundo.Succeeded);
        Assert.Equal(Mensajes.ClienteConPrestamoActivo, segundo.Message);

        using var verificacion = CreateContext();
        Assert.Equal(1, await verificacion.Loans.CountAsync());
    }
}
