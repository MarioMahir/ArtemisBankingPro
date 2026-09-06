using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using Moq;

namespace ArtemisBankingPro.Tests.Unit.Services;

public class SavingsAccountServiceTests
{
    private readonly Mock<IProductNumberGenerator> _numberGenerator = new();
    private readonly Mock<IAccountService> _accountService = new();

    private SavingsAccountService CreateService(
        List<SavingsAccount> accounts, List<Transaction> transactions)
    {
        _numberGenerator.Setup(g => g.GenerateAccountOrLoanNumberAsync()).ReturnsAsync("222222222");
        _accountService.Setup(a => a.GetByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync([]);
        return new SavingsAccountService(
            MockHelpers.Repo(accounts).Object,
            MockHelpers.Repo(transactions).Object,
            _numberGenerator.Object,
            _accountService.Object,
            MockHelpers.UnitOfWork().Object,
            MockHelpers.Mapper());
    }

    private static UserDto Cliente(string id = "cli-1", bool activo = true) => new()
    {
        Id = id,
        FirstName = "Ana",
        LastName = "Pérez",
        Identification = "00112345678",
        Email = "ana@test.com",
        UserName = "ana",
        Role = Roles.Cliente,
        IsActive = activo
    };

    private static SavingsAccount Cuenta(
        int id, string numero, string userId, AccountType tipo,
        decimal balance = 0, ProductStatus estado = ProductStatus.Activa) => new()
    {
        Id = id,
        AccountNumber = numero,
        UserId = userId,
        Type = tipo,
        Balance = balance,
        Status = estado,
        CreatedAt = DateTime.Now
    };

    // ---------------- CreateSecondaryAsync ----------------

    [Fact]
    public async Task CreateSecondaryAsync_ClienteSinCuentaPrincipalActiva_Falla()
    {
        // Arrange: el cliente existe y está activo pero NO tiene principal activa.
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var service = CreateService([], []);

        // Act
        var resultado = await service.CreateSecondaryAsync("cli-1", 100m, "admin-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.RequierePrincipalActiva, resultado.Message);
    }

    [Fact]
    public async Task CreateSecondaryAsync_PrincipalCancelada_Falla()
    {
        // Arrange: tiene principal pero cancelada.
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var cuentas = new List<SavingsAccount>
        {
            Cuenta(1, "111111111", "cli-1", AccountType.Principal, estado: ProductStatus.Cancelada)
        };
        var service = CreateService(cuentas, []);

        // Act
        var resultado = await service.CreateSecondaryAsync("cli-1", 0m, "admin-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.RequierePrincipalActiva, resultado.Message);
    }

    [Fact]
    public async Task CreateSecondaryAsync_ClienteInactivo_Falla()
    {
        // Arrange
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente(activo: false));
        var service = CreateService([], []);

        // Act
        var resultado = await service.CreateSecondaryAsync("cli-1", 0m, "admin-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.SoloCuentasClientesActivos, resultado.Message);
    }

    [Fact]
    public async Task CreateSecondaryAsync_RolNoCliente_Falla()
    {
        // Arrange: usuario activo pero con rol Comercio (tiene principal creada por la API).
        var comercio = Cliente();
        comercio.Role = Roles.Comercio;
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(comercio);
        var cuentas = new List<SavingsAccount>
        {
            Cuenta(1, "111111111", "cli-1", AccountType.Principal)
        };
        var service = CreateService(cuentas, []);

        // Act
        var resultado = await service.CreateSecondaryAsync("cli-1", 0m, "admin-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.SoloCuentasClientesActivos, resultado.Message);
        Assert.Single(cuentas); // no se creó la secundaria
    }

    [Fact]
    public async Task CreateSecondaryAsync_BalanceInicialNegativo_Falla()
    {
        // Arrange
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var cuentas = new List<SavingsAccount> { Cuenta(1, "111111111", "cli-1", AccountType.Principal) };
        var service = CreateService(cuentas, []);

        // Act
        var resultado = await service.CreateSecondaryAsync("cli-1", -50m, "admin-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.BalanceInicialNegativo, resultado.Message);
    }

    [Fact]
    public async Task CreateSecondaryAsync_ConPrincipalActivaYBalancePositivo_CreaCuentaYTransaccionCredito()
    {
        // Arrange
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var cuentas = new List<SavingsAccount> { Cuenta(1, "111111111", "cli-1", AccountType.Principal) };
        var transacciones = new List<Transaction>();
        var service = CreateService(cuentas, transacciones);

        // Act
        var resultado = await service.CreateSecondaryAsync("cli-1", 500m, "admin-1");

        // Assert
        Assert.True(resultado.Succeeded);
        var creada = cuentas.Single(c => c.Type == AccountType.Secundaria);
        Assert.Equal("222222222", creada.AccountNumber);
        Assert.Equal(500m, creada.Balance);
        Assert.Equal(ProductStatus.Activa, creada.Status);
        Assert.Equal("admin-1", creada.AdminUserId);

        var credito = Assert.Single(transacciones);
        Assert.Equal(TransactionType.Credito, credito.Type);
        Assert.Equal(500m, credito.Amount);
        Assert.Equal(TransactionStatus.Aprobada, credito.Status);
    }

    [Fact]
    public async Task CreateSecondaryAsync_BalanceCero_NoRegistraTransaccion()
    {
        // Arrange
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var cuentas = new List<SavingsAccount> { Cuenta(1, "111111111", "cli-1", AccountType.Principal) };
        var transacciones = new List<Transaction>();
        var service = CreateService(cuentas, transacciones);

        // Act
        var resultado = await service.CreateSecondaryAsync("cli-1", 0m, "admin-1");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Empty(transacciones);
    }

    // ---------------- CancelAsync ----------------

    [Fact]
    public async Task CancelAsync_CuentaPrincipal_JamasSePuedeCancelar()
    {
        // Arrange
        var cuentas = new List<SavingsAccount> { Cuenta(1, "111111111", "cli-1", AccountType.Principal, 100m) };
        var service = CreateService(cuentas, []);

        // Act
        var resultado = await service.CancelAsync("111111111");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.PrincipalNoCancelable, resultado.Message);
        Assert.Equal(ProductStatus.Activa, cuentas[0].Status);
    }

    [Fact]
    public async Task CancelAsync_SecundariaConBalance_TransfiereElBalanceALaPrincipalAntesDeCancelar()
    {
        // Arrange
        var principal = Cuenta(1, "111111111", "cli-1", AccountType.Principal, 1_000m);
        var secundaria = Cuenta(2, "222222222", "cli-1", AccountType.Secundaria, 350m);
        var transacciones = new List<Transaction>();
        var service = CreateService([principal, secundaria], transacciones);

        // Act
        var resultado = await service.CancelAsync("222222222");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(1_350m, principal.Balance);
        Assert.Equal(0m, secundaria.Balance);
        Assert.Equal(ProductStatus.Cancelada, secundaria.Status);

        // DÉBITO en la secundaria + CRÉDITO en la principal.
        Assert.Equal(2, transacciones.Count);
        var debito = transacciones.Single(t => t.Type == TransactionType.Debito);
        Assert.Equal(secundaria.Id, debito.AccountId);
        Assert.Equal(350m, debito.Amount);
        Assert.Equal("222222222", debito.Origin);
        Assert.Equal("111111111", debito.Beneficiary);

        var credito = transacciones.Single(t => t.Type == TransactionType.Credito);
        Assert.Equal(principal.Id, credito.AccountId);
        Assert.Equal(350m, credito.Amount);
    }

    [Fact]
    public async Task CancelAsync_SecundariaSinBalance_CancelaSinRegistrarTransacciones()
    {
        // Arrange
        var principal = Cuenta(1, "111111111", "cli-1", AccountType.Principal, 1_000m);
        var secundaria = Cuenta(2, "222222222", "cli-1", AccountType.Secundaria, 0m);
        var transacciones = new List<Transaction>();
        var service = CreateService([principal, secundaria], transacciones);

        // Act
        var resultado = await service.CancelAsync("222222222");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(ProductStatus.Cancelada, secundaria.Status);
        Assert.Empty(transacciones);
        Assert.Equal(1_000m, principal.Balance);
    }

    [Fact]
    public async Task CancelAsync_SinPrincipalActivaParaRecibirFondos_Falla()
    {
        // Arrange: solo existe la secundaria.
        var secundaria = Cuenta(2, "222222222", "cli-1", AccountType.Secundaria, 100m);
        var service = CreateService([secundaria], []);

        // Act
        var resultado = await service.CancelAsync("222222222");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.SinPrincipalParaFondos, resultado.Message);
        Assert.Equal(ProductStatus.Activa, secundaria.Status);
    }

    [Fact]
    public async Task CancelAsync_CuentaYaCancelada_Falla()
    {
        // Arrange
        var secundaria = Cuenta(2, "222222222", "cli-1", AccountType.Secundaria,
            estado: ProductStatus.Cancelada);
        var service = CreateService([secundaria], []);

        // Act
        var resultado = await service.CancelAsync("222222222");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.CuentaYaCancelada, resultado.Message);
    }

    [Fact]
    public async Task CancelAsync_CuentaInexistente_Falla()
    {
        // Arrange
        var service = CreateService([], []);

        // Act
        var resultado = await service.CancelAsync("999999999");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.CuentaNoExiste, resultado.Message);
    }

    // ---------------- GetActiveAccountsByUserAsync ----------------

    [Fact]
    public async Task GetActiveAccountsByUserAsync_PrincipalPrimeroYSecundariasPorBalanceDescendente()
    {
        // Arrange
        var cuentas = new List<SavingsAccount>
        {
            Cuenta(1, "300000003", "cli-1", AccountType.Secundaria, 50m),
            Cuenta(2, "300000001", "cli-1", AccountType.Principal, 10m),
            Cuenta(3, "300000002", "cli-1", AccountType.Secundaria, 900m),
            Cuenta(4, "300000004", "cli-1", AccountType.Secundaria, 0m, ProductStatus.Cancelada),
            Cuenta(5, "300000005", "otro", AccountType.Principal, 99m)
        };
        var service = CreateService(cuentas, []);

        // Act
        var resultado = await service.GetActiveAccountsByUserAsync("cli-1");

        // Assert: solo las activas del cliente, principal primero, secundarias por balance desc.
        Assert.Equal(3, resultado.Count);
        Assert.Equal("300000001", resultado[0].AccountNumber);
        Assert.Equal("300000002", resultado[1].AccountNumber);
        Assert.Equal("300000003", resultado[2].AccountNumber);
    }
}
