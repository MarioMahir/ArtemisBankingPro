using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using Moq;

namespace ArtemisBankingPro.Tests.Unit.Services;

public class UserServiceTests
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<IProductNumberGenerator> _numberGenerator = new();

    private readonly List<SavingsAccount> _accounts = [];
    private readonly List<Transaction> _transactions = [];

    private UserService CreateService()
    {
        _numberGenerator.Setup(g => g.GenerateAccountOrLoanNumberAsync()).ReturnsAsync("123456789");
        return new UserService(
            _accountService.Object,
            MockHelpers.Repo(_accounts).Object,
            MockHelpers.Repo(_transactions).Object,
            _numberGenerator.Object,
            MockHelpers.UnitOfWork().Object);
    }

    private static CreateUserRequest SolicitudCliente(decimal? montoInicial = null) => new()
    {
        FirstName = "Ana", LastName = "Pérez", Identification = "00112345678",
        Email = "ana@test.com", UserName = "ana", Password = "P@ssw0rd!",
        ConfirmPassword = "P@ssw0rd!", Role = Roles.Cliente, InitialAmount = montoInicial
    };

    private void IdentityCreaUsuario(string id = "cli-1", string rol = Roles.Cliente)
    {
        _accountService.Setup(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(ServiceResult<UserDto>.Ok(new UserDto
            {
                Id = id, FirstName = "Ana", LastName = "Pérez", Identification = "00112345678",
                Email = "ana@test.com", UserName = "ana", Role = rol, IsActive = false
            }));
    }

    [Fact]
    public async Task CreateUserAsync_ClienteConMontoInicial_CreaPrincipalAutomaticaConCredito()
    {
        // Arrange
        IdentityCreaUsuario();
        var service = CreateService();

        // Act
        var resultado = await service.CreateUserAsync(SolicitudCliente(5_000m));

        // Assert
        Assert.True(resultado.Succeeded);
        var cuenta = Assert.Single(_accounts);
        Assert.Equal(AccountType.Principal, cuenta.Type);
        Assert.Equal(ProductStatus.Activa, cuenta.Status);
        Assert.Equal(5_000m, cuenta.Balance);
        Assert.Equal("123456789", cuenta.AccountNumber);
        Assert.Equal("cli-1", cuenta.UserId);

        var credito = Assert.Single(_transactions);
        Assert.Equal(TransactionType.Credito, credito.Type);
        Assert.Equal(5_000m, credito.Amount);
    }

    [Fact]
    public async Task CreateUserAsync_ClienteSinMontoInicial_CuentaEnCeroSinTransaccion()
    {
        // Arrange
        IdentityCreaUsuario();
        var service = CreateService();

        // Act
        var resultado = await service.CreateUserAsync(SolicitudCliente(null));

        // Assert
        Assert.True(resultado.Succeeded);
        var cuenta = Assert.Single(_accounts);
        Assert.Equal(0m, cuenta.Balance);
        Assert.Empty(_transactions);
    }

    [Fact]
    public async Task CreateUserAsync_MontoInicialNegativo_Falla()
    {
        // Arrange
        var service = CreateService();

        // Act
        var resultado = await service.CreateUserAsync(SolicitudCliente(-1m));

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.MontoInicialNegativo, resultado.Message);
        _accountService.Verify(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_Cajero_NoCreaCuentaPrincipal()
    {
        // Arrange
        IdentityCreaUsuario(rol: Roles.Cajero);
        var service = CreateService();
        var solicitud = SolicitudCliente();
        solicitud.Role = Roles.Cajero;
        solicitud.InitialAmount = null;

        // Act
        var resultado = await service.CreateUserAsync(solicitud);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Empty(_accounts);
    }

    [Fact]
    public async Task UpdateUserAsync_EditarseASiMismo_Falla()
    {
        // Arrange
        var service = CreateService();

        // Act
        var resultado = await service.UpdateUserAsync(
            new UpdateUserRequest { Id = "admin-1", FirstName = "A", LastName = "B",
                Identification = "1", Email = "a@a.com", UserName = "a" },
            actingUserId: "admin-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.NoEditarPropiaCuenta, resultado.Message);
        _accountService.Verify(a => a.UpdateUserAsync(It.IsAny<UpdateUserRequest>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_MontoAdicional_SeSumaALaPrincipalConCredito()
    {
        // Arrange: ejemplo del spec — balance 5,000 + adicional 12,000 = 17,000.
        _accountService.Setup(a => a.UpdateUserAsync(It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync(ServiceResult.Ok());
        var principal = new SavingsAccount
        {
            Id = 1, AccountNumber = "111111111", UserId = "cli-1", Balance = 5_000m,
            Type = AccountType.Principal, Status = ProductStatus.Activa
        };
        _accounts.Add(principal);
        var service = CreateService();

        // Act
        var resultado = await service.UpdateUserAsync(
            new UpdateUserRequest { Id = "cli-1", FirstName = "A", LastName = "B",
                Identification = "1", Email = "a@a.com", UserName = "a", AdditionalAmount = 12_000m },
            actingUserId: "admin-1");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(17_000m, principal.Balance);
        var credito = Assert.Single(_transactions);
        Assert.Equal(TransactionType.Credito, credito.Type);
        Assert.Equal(12_000m, credito.Amount);
    }

    [Fact]
    public async Task UpdateUserAsync_MontoAdicionalCero_NoTocaElBalance()
    {
        // Arrange
        _accountService.Setup(a => a.UpdateUserAsync(It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync(ServiceResult.Ok());
        var principal = new SavingsAccount
        {
            Id = 1, AccountNumber = "111111111", UserId = "cli-1", Balance = 5_000m,
            Type = AccountType.Principal, Status = ProductStatus.Activa
        };
        _accounts.Add(principal);
        var service = CreateService();

        // Act
        var resultado = await service.UpdateUserAsync(
            new UpdateUserRequest { Id = "cli-1", FirstName = "A", LastName = "B",
                Identification = "1", Email = "a@a.com", UserName = "a", AdditionalAmount = 0m },
            actingUserId: "admin-1");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(5_000m, principal.Balance);
        Assert.Empty(_transactions);
    }
}
