using ArtemisBankingPro.Core.Application.Dtos.Email;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using Moq;

namespace ArtemisBankingPro.Tests.Unit.Services;

public class HermesPayServiceTests
{
    private readonly Mock<IHashingService> _hashingService = new();
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<IEmailService> _emailService = new();

    private readonly List<CreditCard> _cards = [];
    private readonly List<CardConsumption> _consumptions = [];
    private readonly List<Commerce> _commerces = [];
    private readonly List<SavingsAccount> _accounts = [];
    private readonly List<Transaction> _transactions = [];

    private HermesPayService CreateService()
    {
        _hashingService.Setup(h => h.Sha256("123")).Returns("HASH-CORRECTO");
        _hashingService.Setup(h => h.Sha256("999")).Returns("HASH-INCORRECTO");
        _emailService.Setup(e => e.SendAsync(It.IsAny<EmailRequest>())).ReturnsAsync(true);

        Mock<IGenericRepository<Commerce>> commerceRepo = MockHelpers.Repo(_commerces);
        commerceRepo.Setup(r => r.GetByIdAsync(It.IsAny<object[]>()))
            .ReturnsAsync((object[] keys) => _commerces.FirstOrDefault(c => c.Id == (int)keys[0]));

        return new HermesPayService(
            MockHelpers.Repo(_cards).Object,
            MockHelpers.Repo(_consumptions).Object,
            commerceRepo.Object,
            MockHelpers.Repo(_accounts).Object,
            MockHelpers.Repo(_transactions).Object,
            _hashingService.Object,
            _accountService.Object,
            _emailService.Object,
            MockHelpers.UnitOfWork().Object);
    }

    private CreditCard Tarjeta(decimal limite = 1_000m, decimal deuda = 0m)
    {
        var card = new CreditCard
        {
            Id = 1, CardNumber = "5425123456781234", UserId = "cli-1", AdminUserId = "a",
            CreditLimit = limite, OwedAmount = deuda, CvcHash = "HASH-CORRECTO",
            Status = ProductStatus.Activa, ExpirationDate = DateTime.Now.AddYears(3)
        };
        _cards.Add(card);
        return card;
    }

    private (Commerce Comercio, SavingsAccount Cuenta) ComercioConUsuarioYCuenta()
    {
        var commerce = new Commerce
        {
            Id = 5, Name = "Colmado Central", Email = "colmado@test.com",
            PhoneNumber = "8090000000", Rnc = "101000001", IsActive = true
        };
        _commerces.Add(commerce);

        _accountService.Setup(a => a.GetByCommerceIdAsync(5)).ReturnsAsync(new UserDto
        {
            Id = "com-user-1", FirstName = "Comercio", LastName = "Central",
            Identification = "00100000001", Email = "colmado@test.com",
            UserName = "colmado", Role = Roles.Comercio, IsActive = true, CommerceId = 5
        });

        var account = new SavingsAccount
        {
            Id = 20, AccountNumber = "555555555", UserId = "com-user-1", Balance = 0m,
            Type = AccountType.Principal, Status = ProductStatus.Activa
        };
        _accounts.Add(account);
        return (commerce, account);
    }

    private (string Mes, string Anio) Vigencia(CreditCard card) =>
        (card.ExpirationDate.Month.ToString("00"), card.ExpirationDate.Year.ToString());

    [Fact]
    public async Task ProcessPaymentAsync_CvcIncorrecto_RechazaSinRegistrarNada()
    {
        // Arrange
        var tarjeta = Tarjeta();
        ComercioConUsuarioYCuenta();
        var (mes, anio) = Vigencia(tarjeta);
        var service = CreateService();

        // Act: CVC "999" no coincide con el hash almacenado.
        var resultado = await service.ProcessPaymentAsync(5, tarjeta.CardNumber, mes, anio, "999", 100m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.TarjetaInvalidaCajero, resultado.Message);
        Assert.Equal(0m, tarjeta.OwedAmount);
        Assert.Empty(_consumptions);
        Assert.Empty(_transactions);
    }

    [Fact]
    public async Task ProcessPaymentAsync_CreditoInsuficiente_ConsumoRechazadoSinTocarDeudaNiBalances()
    {
        // Arrange: disponible = 1,000 − 900 = 100 < 500.
        var tarjeta = Tarjeta(limite: 1_000m, deuda: 900m);
        var (_, cuentaComercio) = ComercioConUsuarioYCuenta();
        var (mes, anio) = Vigencia(tarjeta);
        var service = CreateService();

        // Act
        var resultado = await service.ProcessPaymentAsync(5, tarjeta.CardNumber, mes, anio, "123", 500m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.ApiExcedeCreditoDisponible, resultado.Message);
        Assert.Equal(900m, tarjeta.OwedAmount);   // la deuda NO cambia
        Assert.Equal(0m, cuentaComercio.Balance); // el comercio NO recibe nada
        Assert.Empty(_transactions);

        var consumo = Assert.Single(_consumptions);
        Assert.Equal(ConsumptionStatus.Rechazado, consumo.Status);
        Assert.Equal(500m, consumo.Amount);
        Assert.Equal("Colmado Central", consumo.CommerceName);
    }

    [Fact]
    public async Task ProcessPaymentAsync_PagoAprobado_AcreditaAlComercioYSubeLaDeuda()
    {
        // Arrange
        var tarjeta = Tarjeta(limite: 1_000m, deuda: 200m);
        var (_, cuentaComercio) = ComercioConUsuarioYCuenta();
        var (mes, anio) = Vigencia(tarjeta);
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync((UserDto?)null);
        var service = CreateService();

        // Act
        var resultado = await service.ProcessPaymentAsync(5, tarjeta.CardNumber, mes, anio, "123", 300m);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(500m, tarjeta.OwedAmount);      // deuda sube 300
        Assert.Equal(300m, cuentaComercio.Balance);  // CRÉDITO al comercio

        var consumo = Assert.Single(_consumptions);
        Assert.Equal(ConsumptionStatus.Aprobado, consumo.Status);
        Assert.Equal(300m, consumo.Amount);

        var credito = Assert.Single(_transactions);
        Assert.Equal(TransactionType.Credito, credito.Type);
        Assert.Equal(cuentaComercio.Id, credito.AccountId);
        Assert.Equal("1234", credito.Origin);           // origen = últimos 4
        Assert.Equal("555555555", credito.Beneficiary); // cuenta principal del comercio
    }

    [Fact]
    public async Task ProcessPaymentAsync_MontoIgualAlDisponible_SeAprueba()
    {
        // Arrange: disponible exacto.
        var tarjeta = Tarjeta(limite: 1_000m, deuda: 700m);
        ComercioConUsuarioYCuenta();
        var (mes, anio) = Vigencia(tarjeta);
        var service = CreateService();

        // Act
        var resultado = await service.ProcessPaymentAsync(5, tarjeta.CardNumber, mes, anio, "123", 300m);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(1_000m, tarjeta.OwedAmount);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ComercioInactivo_Falla()
    {
        // Arrange
        var tarjeta = Tarjeta();
        var (comercio, _) = ComercioConUsuarioYCuenta();
        comercio.IsActive = false;
        var (mes, anio) = Vigencia(tarjeta);
        var service = CreateService();

        // Act
        var resultado = await service.ProcessPaymentAsync(5, tarjeta.CardNumber, mes, anio, "123", 100m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.ComercioInactivo, resultado.Message);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ComercioSinUsuario_Falla()
    {
        // Arrange
        var tarjeta = Tarjeta();
        _commerces.Add(new Commerce
        {
            Id = 5, Name = "Sin Usuario", Email = "x@test.com", PhoneNumber = "809",
            Rnc = "101", IsActive = true
        });
        _accountService.Setup(a => a.GetByCommerceIdAsync(5)).ReturnsAsync((UserDto?)null);
        var (mes, anio) = Vigencia(tarjeta);
        var service = CreateService();

        // Act
        var resultado = await service.ProcessPaymentAsync(5, tarjeta.CardNumber, mes, anio, "123", 100m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.ComercioSinUsuario, resultado.Message);
    }

    [Fact]
    public async Task ProcessPaymentAsync_VigenciaQueNoCoincideConLaTarjeta_Falla()
    {
        // Arrange
        var tarjeta = Tarjeta();
        ComercioConUsuarioYCuenta();
        var service = CreateService();

        // Act: mes/año que no corresponden a la tarjeta.
        var resultado = await service.ProcessPaymentAsync(
            5, tarjeta.CardNumber, "01", (tarjeta.ExpirationDate.Year + 1).ToString(), "123", 100m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.TarjetaInvalidaCajero, resultado.Message);
    }

    [Fact]
    public async Task ProcessPaymentAsync_TarjetaCancelada_Falla()
    {
        // Arrange
        var tarjeta = Tarjeta();
        tarjeta.Status = ProductStatus.Cancelada;
        ComercioConUsuarioYCuenta();
        var (mes, anio) = Vigencia(tarjeta);
        var service = CreateService();

        // Act
        var resultado = await service.ProcessPaymentAsync(5, tarjeta.CardNumber, mes, anio, "123", 100m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.TarjetaNoActiva, resultado.Message);
    }
}
