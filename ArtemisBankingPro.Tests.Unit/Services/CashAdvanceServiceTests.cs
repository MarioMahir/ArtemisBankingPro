using ArtemisBankingPro.Core.Application.Dtos.Email;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using Moq;

namespace ArtemisBankingPro.Tests.Unit.Services;

public class CashAdvanceServiceTests
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<IEmailService> _emailService = new();

    private readonly List<CreditCard> _cards = [];
    private readonly List<CardConsumption> _consumptions = [];
    private readonly List<SavingsAccount> _accounts = [];
    private readonly List<Transaction> _transactions = [];

    private CashAdvanceService CreateService()
    {
        _emailService.Setup(e => e.SendAsync(It.IsAny<EmailRequest>())).ReturnsAsync(true);
        _accountService.Setup(a => a.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((UserDto?)null);
        return new CashAdvanceService(
            MockHelpers.Repo(_cards).Object,
            MockHelpers.Repo(_consumptions).Object,
            MockHelpers.Repo(_accounts).Object,
            MockHelpers.Repo(_transactions).Object,
            _accountService.Object,
            _emailService.Object,
            MockHelpers.UnitOfWork().Object);
    }

    private CreditCard Tarjeta(decimal limite = 500m, decimal deuda = 300m)
    {
        var card = new CreditCard
        {
            Id = 1, CardNumber = "5425123456781234", UserId = "cli-1", AdminUserId = "a",
            CreditLimit = limite, OwedAmount = deuda, CvcHash = "h",
            Status = ProductStatus.Activa, ExpirationDate = DateTime.Now.AddYears(3)
        };
        _cards.Add(card);
        return card;
    }

    private SavingsAccount CuentaDestino(decimal balance = 0m)
    {
        var cuenta = new SavingsAccount
        {
            Id = 1, AccountNumber = "111111111", UserId = "cli-1", Balance = balance,
            Type = AccountType.Principal, Status = ProductStatus.Activa
        };
        _accounts.Add(cuenta);
        return cuenta;
    }

    [Fact]
    public async Task ExecuteAsync_Avance200ConDisponible200_RechazadoSinTocarNada()
    {
        // Arrange: ejemplo EXACTO del spec (límite 500, deuda 300, avance 200 → total 212.50).
        var tarjeta = Tarjeta();
        var cuenta = CuentaDestino(50m);
        var service = CreateService();

        // Act
        var resultado = await service.ExecuteAsync("cli-1", 1, "111111111", 200m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.AvanceExcedeDisponible, resultado.Message);

        // NADA se toca: ni deuda, ni balance, ni transacciones.
        Assert.Equal(300m, tarjeta.OwedAmount);
        Assert.Equal(50m, cuenta.Balance);
        Assert.Empty(_transactions);

        // Se registra el consumo RECHAZADO por el total (avance + interés).
        var consumo = Assert.Single(_consumptions);
        Assert.Equal(ConsumptionStatus.Rechazado, consumo.Status);
        Assert.Equal(212.50m, consumo.Amount);
        Assert.Equal(AppConstants.CashAdvanceCommerceName, consumo.CommerceName);
    }

    [Fact]
    public async Task ExecuteAsync_Avance100ConDisponible200_AprobadoCargaTotalYAcreditaSoloElAvance()
    {
        // Arrange: ejemplo del spec — avance 100 → interés 6.25, total 106.25.
        var tarjeta = Tarjeta();
        var cuenta = CuentaDestino(0m);
        var service = CreateService();

        // Act
        var resultado = await service.ExecuteAsync("cli-1", 1, "111111111", 100m);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(406.25m, tarjeta.OwedAmount); // 300 + 106.25
        Assert.Equal(100m, cuenta.Balance);        // la cuenta recibe SOLO el avance

        var consumo = Assert.Single(_consumptions);
        Assert.Equal(ConsumptionStatus.Aprobado, consumo.Status);
        Assert.Equal(106.25m, consumo.Amount); // el consumo AVANCE es por el TOTAL
        Assert.Equal(AppConstants.CashAdvanceCommerceName, consumo.CommerceName);

        var credito = Assert.Single(_transactions);
        Assert.Equal(TransactionType.Credito, credito.Type);
        Assert.Equal(100m, credito.Amount);
        Assert.Equal("1234", credito.Origin); // origen = últimos 4 de la tarjeta
    }

    [Fact]
    public async Task ExecuteAsync_TarjetaVencida_Falla()
    {
        // Arrange
        var tarjeta = Tarjeta();
        tarjeta.ExpirationDate = DateTime.Now.AddDays(-1);
        CuentaDestino();
        var service = CreateService();

        // Act
        var resultado = await service.ExecuteAsync("cli-1", 1, "111111111", 100m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.TarjetaVencida, resultado.Message);
        Assert.Empty(_consumptions);
    }

    [Fact]
    public async Task ExecuteAsync_MontoCeroONegativo_Falla()
    {
        // Arrange
        Tarjeta();
        CuentaDestino();
        var service = CreateService();

        // Act
        var resultado = await service.ExecuteAsync("cli-1", 1, "111111111", 0m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.MontoAvanceInvalido, resultado.Message);
    }

    [Fact]
    public async Task ExecuteAsync_TarjetaDeOtroUsuario_Falla()
    {
        // Arrange
        var tarjeta = Tarjeta();
        tarjeta.UserId = "otro";
        CuentaDestino();
        var service = CreateService();

        // Act
        var resultado = await service.ExecuteAsync("cli-1", 1, "111111111", 100m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.TarjetaNoExiste, resultado.Message);
    }

    [Fact]
    public async Task PrepareAsync_DevuelveAvanceInteresYTotalParaLaConfirmacion()
    {
        // Arrange
        Tarjeta();
        CuentaDestino();
        var service = CreateService();

        // Act
        var resultado = await service.PrepareAsync("cli-1", 1, "111111111", 100m);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(100m, resultado.Data!.AdvanceAmount);
        Assert.Equal(6.25m, resultado.Data.InterestAmount);
        Assert.Equal(106.25m, resultado.Data.TotalToCharge);
        Assert.Equal(200m, resultado.Data.AvailableCredit);
        Assert.Equal("1234", resultado.Data.CardLastFourDigits);
    }
}
