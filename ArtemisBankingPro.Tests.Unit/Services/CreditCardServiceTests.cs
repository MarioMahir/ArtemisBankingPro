using ArtemisBankingPro.Core.Application.Dtos.Email;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using Moq;

namespace ArtemisBankingPro.Tests.Unit.Services;

public class CreditCardServiceTests
{
    private readonly Mock<IProductNumberGenerator> _numberGenerator = new();
    private readonly Mock<IHashingService> _hashingService = new();
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<IEmailService> _emailService = new();

    private readonly List<CreditCard> _cards = [];
    private readonly List<CardConsumption> _consumptions = [];

    private CreditCardService CreateService()
    {
        _numberGenerator.Setup(g => g.GenerateCardNumberAsync()).ReturnsAsync("5425123456781234");
        _numberGenerator.Setup(g => g.GenerateCvc()).Returns("987");
        _hashingService.Setup(h => h.Sha256("987")).Returns("HASH-DEL-CVC");
        _emailService.Setup(e => e.SendAsync(It.IsAny<EmailRequest>())).ReturnsAsync(true);
        _accountService.Setup(a => a.GetByIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync([]);
        return new CreditCardService(
            MockHelpers.Repo(_cards).Object,
            MockHelpers.Repo(_consumptions).Object,
            _numberGenerator.Object,
            _hashingService.Object,
            _accountService.Object,
            _emailService.Object,
            MockHelpers.UnitOfWork().Object,
            MockHelpers.Mapper());
    }

    private static UserDto Cliente(bool activo = true) => new()
    {
        Id = "cli-1",
        FirstName = "Ana",
        LastName = "Pérez",
        Identification = "00112345678",
        Email = "ana@test.com",
        UserName = "ana",
        Role = Roles.Cliente,
        IsActive = activo
    };

    private CreditCard Tarjeta(
        decimal deuda = 0m, decimal limite = 10_000m, ProductStatus estado = ProductStatus.Activa)
    {
        var card = new CreditCard
        {
            Id = 1,
            CardNumber = "5425123456781234",
            UserId = "cli-1",
            AdminUserId = "admin-1",
            CreditLimit = limite,
            OwedAmount = deuda,
            ExpirationDate = DateTime.Now.AddYears(3),
            CvcHash = "HASH-DEL-CVC",
            Status = estado,
            CreatedAt = DateTime.Now
        };
        _cards.Add(card);
        return card;
    }

    // ---------------- AssignAsync ----------------

    [Fact]
    public async Task AssignAsync_ClienteActivo_PersisteSoloElHashDelCvc()
    {
        // Arrange
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var service = CreateService();

        // Act
        var resultado = await service.AssignAsync("cli-1", 5_000m, "admin-1");

        // Assert
        Assert.True(resultado.Succeeded);
        var tarjeta = Assert.Single(_cards);

        // El CVC en claro ("987") JAMÁS se persiste: solo su hash SHA-256.
        Assert.Equal("HASH-DEL-CVC", tarjeta.CvcHash);
        Assert.DoesNotContain("987", tarjeta.CvcHash.Replace("HASH-DEL-CVC", "HASH-DEL-CVC"));
        Assert.NotEqual("987", tarjeta.CvcHash);
        _hashingService.Verify(h => h.Sha256("987"), Times.Once);

        Assert.Equal(0m, tarjeta.OwedAmount); // deuda inicial RD$0.00
        Assert.Equal(ProductStatus.Activa, tarjeta.Status);
        Assert.Equal("admin-1", tarjeta.AdminUserId);
    }

    [Fact]
    public async Task AssignAsync_RolNoCliente_Falla()
    {
        // Arrange: usuario activo pero con rol Cajero.
        var cajero = Cliente();
        cajero.Role = Roles.Cajero;
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(cajero);
        var service = CreateService();

        // Act
        var resultado = await service.AssignAsync("cli-1", 5_000m, "admin-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.SoloTarjetasClientesActivos, resultado.Message);
        Assert.Empty(_cards);
    }

    [Fact]
    public async Task AssignAsync_ExpiracionEsTresAniosDespues()
    {
        // Arrange
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var service = CreateService();

        // Act
        await service.AssignAsync("cli-1", 5_000m, "admin-1");

        // Assert
        var tarjeta = Assert.Single(_cards);
        Assert.Equal(DateTime.Now.AddYears(3).Year, tarjeta.ExpirationDate.Year);
    }

    [Fact]
    public async Task AssignAsync_CorreoNuncaIncluyeCvcNiNumeroCompleto()
    {
        // Arrange
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var service = CreateService();
        EmailRequest? correo = null;
        _emailService.Setup(e => e.SendAsync(It.IsAny<EmailRequest>()))
            .Callback<EmailRequest>(r => correo = r)
            .ReturnsAsync(true);

        // Act
        await service.AssignAsync("cli-1", 5_000m, "admin-1");

        // Assert
        Assert.NotNull(correo);
        Assert.DoesNotContain("987", correo!.HtmlBody);              // CVC
        Assert.DoesNotContain("5425123456781234", correo.HtmlBody);  // número completo
        Assert.Contains("1234", correo.HtmlBody);                    // últimos 4 sí
    }

    [Fact]
    public async Task AssignAsync_ClienteInactivo_Falla()
    {
        // Arrange
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente(activo: false));
        var service = CreateService();

        // Act
        var resultado = await service.AssignAsync("cli-1", 5_000m, "admin-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.SoloTarjetasClientesActivos, resultado.Message);
        Assert.Empty(_cards);
    }

    [Fact]
    public async Task AssignAsync_LimiteCeroONegativo_Falla()
    {
        // Arrange
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var service = CreateService();

        // Act
        var resultado = await service.AssignAsync("cli-1", 0m, "admin-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.LimiteInvalido, resultado.Message);
    }

    // ---------------- CancelAsync ----------------

    [Fact]
    public async Task CancelAsync_TarjetaConDeuda_Falla()
    {
        // Arrange
        var tarjeta = Tarjeta(deuda: 100m);
        var service = CreateService();

        // Act
        var resultado = await service.CancelAsync(1);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.TarjetaConDeudaNoCancelable, resultado.Message);
        Assert.Equal(ProductStatus.Activa, tarjeta.Status);
    }

    [Fact]
    public async Task CancelAsync_TarjetaSinDeuda_SeCancela()
    {
        // Arrange
        var tarjeta = Tarjeta(deuda: 0m);
        var service = CreateService();

        // Act
        var resultado = await service.CancelAsync(1);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(ProductStatus.Cancelada, tarjeta.Status);
    }

    [Fact]
    public async Task CancelAsync_TarjetaYaCancelada_Falla()
    {
        // Arrange
        Tarjeta(estado: ProductStatus.Cancelada);
        var service = CreateService();

        // Act
        var resultado = await service.CancelAsync(1);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.TarjetaCanceladaNoModificable, resultado.Message);
    }

    // ---------------- UpdateLimitAsync ----------------

    [Fact]
    public async Task UpdateLimitAsync_NuevoLimiteMenorQueLaDeuda_Falla()
    {
        // Arrange
        var tarjeta = Tarjeta(deuda: 5_000m, limite: 10_000m);
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var service = CreateService();

        // Act
        var resultado = await service.UpdateLimitAsync(1, 4_000m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.LimiteMenorQueDeuda, resultado.Message);
        Assert.Equal(10_000m, tarjeta.CreditLimit);
    }

    [Fact]
    public async Task UpdateLimitAsync_NuevoLimiteIgualALaDeuda_SeAcepta()
    {
        // Arrange: la regla es "nuevo límite ≥ deuda".
        var tarjeta = Tarjeta(deuda: 5_000m, limite: 10_000m);
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var service = CreateService();

        // Act
        var resultado = await service.UpdateLimitAsync(1, 5_000m);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(5_000m, tarjeta.CreditLimit);
    }

    [Fact]
    public async Task UpdateLimitAsync_TarjetaCancelada_Falla()
    {
        // Arrange
        Tarjeta(estado: ProductStatus.Cancelada);
        var service = CreateService();

        // Act
        var resultado = await service.UpdateLimitAsync(1, 8_000m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.TarjetaCanceladaNoModificable, resultado.Message);
    }

    [Fact]
    public async Task UpdateLimitAsync_LimiteCero_Falla()
    {
        // Arrange
        Tarjeta();
        var service = CreateService();

        // Act
        var resultado = await service.UpdateLimitAsync(1, 0m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.LimiteTarjetaInvalido, resultado.Message);
    }

    // ---------------- DTOs enmascarados ----------------

    [Fact]
    public async Task GetDetailAsync_ElDtoSoloExponeElNumeroEnmascarado()
    {
        // Arrange
        Tarjeta();
        var service = CreateService();

        // Act
        var resultado = await service.GetDetailAsync(1);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal("***********1234", resultado.Data!.MaskedCardNumber);
        Assert.Equal("1234", resultado.Data.LastFourDigits);
    }
}
