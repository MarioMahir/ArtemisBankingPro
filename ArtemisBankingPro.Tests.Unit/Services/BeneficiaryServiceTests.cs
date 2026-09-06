using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using Moq;

namespace ArtemisBankingPro.Tests.Unit.Services;

public class BeneficiaryServiceTests
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly List<Beneficiary> _beneficiaries = [];
    private readonly List<SavingsAccount> _accounts = [];

    private BeneficiaryService CreateService()
    {
        _accountService.Setup(a => a.GetByIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync([]);
        return new BeneficiaryService(
            MockHelpers.Repo(_beneficiaries).Object,
            MockHelpers.Repo(_accounts).Object,
            _accountService.Object,
            MockHelpers.UnitOfWork().Object);
    }

    private SavingsAccount CuentaAjena(
        int id = 1, string numero = "222222222", string dueno = "otro-cliente",
        ProductStatus estado = ProductStatus.Activa)
    {
        var cuenta = new SavingsAccount
        {
            Id = id, AccountNumber = numero, UserId = dueno,
            Type = AccountType.Principal, Status = estado
        };
        _accounts.Add(cuenta);
        return cuenta;
    }

    [Fact]
    public async Task AddAsync_CuentaPropia_NoPuedeSerBeneficiario()
    {
        // Arrange: la cuenta pertenece al MISMO cliente.
        CuentaAjena(dueno: "cli-1");
        var service = CreateService();

        // Act
        var resultado = await service.AddAsync("cli-1", "222222222");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.BeneficiarioPropio, resultado.Message);
        Assert.Empty(_beneficiaries);
    }

    [Fact]
    public async Task AddAsync_BeneficiarioDuplicado_Falla()
    {
        // Arrange
        var cuenta = CuentaAjena();
        _beneficiaries.Add(new Beneficiary { Id = 1, UserId = "cli-1", SavingsAccountId = cuenta.Id });
        var service = CreateService();

        // Act
        var resultado = await service.AddAsync("cli-1", "222222222");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.BeneficiarioDuplicado, resultado.Message);
        Assert.Single(_beneficiaries);
    }

    [Fact]
    public async Task AddAsync_CuentaCancelada_Falla()
    {
        // Arrange
        CuentaAjena(estado: ProductStatus.Cancelada);
        var service = CreateService();

        // Act
        var resultado = await service.AddAsync("cli-1", "222222222");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.BeneficiarioCancelado, resultado.Message);
    }

    [Fact]
    public async Task AddAsync_CuentaInexistente_Falla()
    {
        // Arrange
        var service = CreateService();

        // Act
        var resultado = await service.AddAsync("cli-1", "999999999");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.CuentaInvalida, resultado.Message);
    }

    [Theory]
    [InlineData("12345")]        // menos de 9 dígitos
    [InlineData("1234567890")]   // más de 9 dígitos
    [InlineData("12345678A")]    // no numérico
    public async Task AddAsync_FormatoDeNumeroInvalido_Falla(string numero)
    {
        // Arrange
        var service = CreateService();

        // Act
        var resultado = await service.AddAsync("cli-1", numero);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.CuentaInvalida, resultado.Message);
    }

    [Fact]
    public async Task AddAsync_CuentaAjenaActivaNoDuplicada_SeAgrega()
    {
        // Arrange
        var cuenta = CuentaAjena();
        var service = CreateService();

        // Act
        var resultado = await service.AddAsync("cli-1", "222222222");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(Mensajes.BeneficiarioAgregado, resultado.Message);
        var beneficiario = Assert.Single(_beneficiaries);
        Assert.Equal("cli-1", beneficiario.UserId);
        Assert.Equal(cuenta.Id, beneficiario.SavingsAccountId);
    }

    [Fact]
    public async Task RemoveAsync_BeneficiarioPropio_EliminaSoloLaRelacion()
    {
        // Arrange
        var cuenta = CuentaAjena();
        _beneficiaries.Add(new Beneficiary { Id = 7, UserId = "cli-1", SavingsAccountId = cuenta.Id });
        var service = CreateService();

        // Act
        var resultado = await service.RemoveAsync("cli-1", 7);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(Mensajes.BeneficiarioEliminado, resultado.Message);
        Assert.Empty(_beneficiaries);
        Assert.Single(_accounts); // la cuenta NO se toca
    }

    [Fact]
    public async Task RemoveAsync_BeneficiarioDeOtroUsuario_Falla()
    {
        // Arrange
        var cuenta = CuentaAjena();
        _beneficiaries.Add(new Beneficiary { Id = 7, UserId = "otro", SavingsAccountId = cuenta.Id });
        var service = CreateService();

        // Act
        var resultado = await service.RemoveAsync("cli-1", 7);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.BeneficiarioNoDisponible, resultado.Message);
        Assert.Single(_beneficiaries);
    }
}
