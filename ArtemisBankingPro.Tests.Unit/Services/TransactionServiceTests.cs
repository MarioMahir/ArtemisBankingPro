using ArtemisBankingPro.Core.Application.Dtos.Email;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using Moq;

namespace ArtemisBankingPro.Tests.Unit.Services;

public class TransactionServiceTests
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<IEmailService> _emailService = new();

    private readonly List<SavingsAccount> _accounts = [];
    private readonly List<Transaction> _transactions = [];
    private readonly List<CreditCard> _cards = [];
    private readonly List<Loan> _loans = [];
    private readonly List<LoanInstallment> _installments = [];
    private readonly List<Beneficiary> _beneficiaries = [];

    private TransactionService CreateService()
    {
        _emailService.Setup(e => e.SendAsync(It.IsAny<EmailRequest>())).ReturnsAsync(true);
        _accountService.Setup(a => a.GetByIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync([]);
        _accountService.Setup(a => a.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((UserDto?)null);
        return new TransactionService(
            MockHelpers.Repo(_accounts).Object,
            MockHelpers.Repo(_transactions).Object,
            MockHelpers.Repo(_cards).Object,
            MockHelpers.Repo(_loans).Object,
            MockHelpers.Repo(_installments).Object,
            MockHelpers.Repo(_beneficiaries).Object,
            _accountService.Object,
            _emailService.Object,
            MockHelpers.UnitOfWork().Object);
    }

    private SavingsAccount Cuenta(int id, string numero, string userId, decimal balance,
        ProductStatus estado = ProductStatus.Activa)
    {
        var cuenta = new SavingsAccount
        {
            Id = id, AccountNumber = numero, UserId = userId, Balance = balance,
            Type = AccountType.Principal, Status = estado, CreatedAt = DateTime.Now
        };
        _accounts.Add(cuenta);
        return cuenta;
    }

    // ---------------- Transferencia express ----------------

    [Fact]
    public async Task ExpressTransferAsync_FondosInsuficientes_RegistraRechazadaSinTocarBalances()
    {
        // Arrange
        var origen = Cuenta(1, "111111111", "cli-1", 100m);
        var destino = Cuenta(2, "222222222", "cli-2", 0m);
        var service = CreateService();

        // Act
        var resultado = await service.ExpressTransferAsync("111111111", "222222222", 500m, ownerUserId: "cli-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.FondosInsuficientes, resultado.Message);

        // Los balances NO se tocan.
        Assert.Equal(100m, origen.Balance);
        Assert.Equal(0m, destino.Balance);

        // Se registra la transacción RECHAZADA en la cuenta origen.
        var rechazada = Assert.Single(_transactions);
        Assert.Equal(TransactionStatus.Rechazada, rechazada.Status);
        Assert.Equal(TransactionType.Debito, rechazada.Type);
        Assert.Equal(origen.Id, rechazada.AccountId);
        Assert.Equal(500m, rechazada.Amount);
        Assert.False(rechazada.IsPayment);
    }

    [Fact]
    public async Task ExpressTransferAsync_ConFondos_RegistraDebitoYCreditoAprobados()
    {
        // Arrange
        var origen = Cuenta(1, "111111111", "cli-1", 1_000m);
        var destino = Cuenta(2, "222222222", "cli-2", 200m);
        var service = CreateService();

        // Act
        var resultado = await service.ExpressTransferAsync("111111111", "222222222", 300m, ownerUserId: "cli-1");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(700m, origen.Balance);
        Assert.Equal(500m, destino.Balance);

        Assert.Equal(2, _transactions.Count);
        var debito = _transactions.Single(t => t.Type == TransactionType.Debito);
        Assert.Equal(origen.Id, debito.AccountId);
        Assert.Equal("222222222", debito.Beneficiary);
        Assert.Equal("111111111", debito.Origin);
        Assert.Equal(TransactionStatus.Aprobada, debito.Status);

        var credito = _transactions.Single(t => t.Type == TransactionType.Credito);
        Assert.Equal(destino.Id, credito.AccountId);
        Assert.Equal(TransactionStatus.Aprobada, credito.Status);
    }

    [Fact]
    public async Task ExpressTransferAsync_OrigenIgualDestino_Falla()
    {
        // Arrange
        Cuenta(1, "111111111", "cli-1", 1_000m);
        var service = CreateService();

        // Act
        var resultado = await service.ExpressTransferAsync("111111111", "111111111", 100m, ownerUserId: "cli-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.DestinoIgualOrigen, resultado.Message);
        Assert.Empty(_transactions);
    }

    [Fact]
    public async Task ExpressTransferAsync_CuentaAjenaComoOrigen_Falla()
    {
        // Arrange: la cuenta origen pertenece a otro usuario.
        Cuenta(1, "111111111", "otro-usuario", 1_000m);
        Cuenta(2, "222222222", "cli-2", 0m);
        var service = CreateService();

        // Act
        var resultado = await service.ExpressTransferAsync("111111111", "222222222", 100m, ownerUserId: "cli-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.CuentaInvalida, resultado.Message);
    }

    // ---------------- Pago a tarjeta ----------------

    [Fact]
    public async Task PayCreditCardAsync_PagoMayorQueLaDeuda_SoloDebitaElMontoEfectivo()
    {
        // Arrange: deuda 500, pago 1,000 → efectivo 500 (anti-sobrepago del spec).
        var origen = Cuenta(1, "111111111", "cli-1", 2_000m);
        var tarjeta = new CreditCard
        {
            Id = 1, CardNumber = "5425123456781234", UserId = "cli-1", AdminUserId = "a",
            CreditLimit = 10_000m, OwedAmount = 500m, CvcHash = "h",
            Status = ProductStatus.Activa, ExpirationDate = DateTime.Now.AddYears(3)
        };
        _cards.Add(tarjeta);
        var service = CreateService();

        // Act
        var resultado = await service.PayCreditCardAsync("111111111", "5425123456781234", 1_000m, ownerUserId: "cli-1");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(1_500m, origen.Balance);  // solo se debitan 500
        Assert.Equal(0m, tarjeta.OwedAmount);

        var pago = Assert.Single(_transactions);
        Assert.Equal(500m, pago.Amount);
        Assert.True(pago.IsPayment); // marcado como pago para los indicadores
        Assert.Equal("1234", pago.Beneficiary); // últimos 4 de la tarjeta
        Assert.Equal(TransactionType.Debito, pago.Type);
    }

    [Fact]
    public async Task PayCreditCardAsync_SinFondosParaElEfectivo_RegistraRechazadaConIsPayment()
    {
        // Arrange
        var origen = Cuenta(1, "111111111", "cli-1", 100m);
        var tarjeta = new CreditCard
        {
            Id = 1, CardNumber = "5425123456781234", UserId = "cli-1", AdminUserId = "a",
            CreditLimit = 10_000m, OwedAmount = 500m, CvcHash = "h",
            Status = ProductStatus.Activa, ExpirationDate = DateTime.Now.AddYears(3)
        };
        _cards.Add(tarjeta);
        var service = CreateService();

        // Act
        var resultado = await service.PayCreditCardAsync("111111111", "5425123456781234", 500m, ownerUserId: "cli-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.SinFondosParaTransaccion, resultado.Message);
        Assert.Equal(100m, origen.Balance);
        Assert.Equal(500m, tarjeta.OwedAmount);

        var rechazada = Assert.Single(_transactions);
        Assert.Equal(TransactionStatus.Rechazada, rechazada.Status);
        Assert.True(rechazada.IsPayment);
    }

    [Fact]
    public async Task PayCreditCardAsync_TarjetaSinDeuda_Falla()
    {
        // Arrange
        Cuenta(1, "111111111", "cli-1", 2_000m);
        _cards.Add(new CreditCard
        {
            Id = 1, CardNumber = "5425123456781234", UserId = "cli-1", AdminUserId = "a",
            CreditLimit = 10_000m, OwedAmount = 0m, CvcHash = "h",
            Status = ProductStatus.Activa, ExpirationDate = DateTime.Now.AddYears(3)
        });
        var service = CreateService();

        // Act
        var resultado = await service.PayCreditCardAsync("111111111", "5425123456781234", 100m, ownerUserId: "cli-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.TarjetaSinDeuda, resultado.Message);
    }

    // ---------------- Pago a préstamo ----------------

    private Loan PrestamoConCuotas(params decimal[] pendientes)
    {
        var loan = new Loan
        {
            Id = 1, LoanNumber = "900000001", UserId = "cli-1", AdminUserId = "a",
            ApprovedAmount = pendientes.Sum(), TermMonths = pendientes.Length,
            Status = LoanStatus.Activo, CreatedAt = DateTime.Now
        };
        _loans.Add(loan);
        for (var i = 0; i < pendientes.Length; i++)
        {
            _installments.Add(new LoanInstallment
            {
                Id = i + 1, LoanId = 1, Number = i + 1,
                DueDate = DateTime.Today.AddMonths(i + 1),
                Value = pendientes[i], PendingAmount = pendientes[i],
                Status = pendientes[i] == 0 ? InstallmentStatus.Pagada : InstallmentStatus.Pendiente
            });
        }
        return loan;
    }

    [Fact]
    public async Task PayLoanAsync_PagoMayorQueLoPendiente_AntiSobrepagoYPrestamoCompletado()
    {
        // Arrange: pendiente 2,000, pago 3,000 → efectivo 2,000 (ejemplo del spec).
        var origen = Cuenta(1, "111111111", "cli-1", 5_000m);
        var prestamo = PrestamoConCuotas(1_000m, 1_000m);
        var service = CreateService();

        // Act
        var resultado = await service.PayLoanAsync("111111111", "900000001", 3_000m, ownerUserId: "cli-1");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(3_000m, origen.Balance); // solo se debitan 2,000
        Assert.All(_installments, i => Assert.Equal(InstallmentStatus.Pagada, i.Status));
        Assert.Equal(LoanStatus.Completado, prestamo.Status); // todas pagadas → Completado

        var pago = Assert.Single(_transactions);
        Assert.Equal(2_000m, pago.Amount);
        Assert.True(pago.IsPayment);
        Assert.Equal("900000001", pago.Beneficiary); // beneficiario = nº de préstamo
    }

    [Fact]
    public async Task PayLoanAsync_PagoParcial_CascadaSobreLaCuotaMasAntigua()
    {
        // Arrange
        Cuenta(1, "111111111", "cli-1", 5_000m);
        var prestamo = PrestamoConCuotas(1_000m, 1_000m);
        var service = CreateService();

        // Act: 1,500 → 1ª pagada, 2ª parcialmente pagada.
        var resultado = await service.PayLoanAsync("111111111", "900000001", 1_500m, ownerUserId: "cli-1");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(InstallmentStatus.Pagada, _installments[0].Status);
        Assert.Equal(InstallmentStatus.ParcialmentePagada, _installments[1].Status);
        Assert.Equal(500m, _installments[1].PendingAmount);
        Assert.Equal(LoanStatus.Activo, prestamo.Status);
    }

    [Fact]
    public async Task PayLoanAsync_PrestamoSinCuotasPendientes_Falla()
    {
        // Arrange
        Cuenta(1, "111111111", "cli-1", 5_000m);
        PrestamoConCuotas(0m, 0m);
        var service = CreateService();

        // Act
        var resultado = await service.PayLoanAsync("111111111", "900000001", 100m, ownerUserId: "cli-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.PrestamoSinCuotasPendientes, resultado.Message);
    }

    // ---------------- Depósito y retiro (cajero) ----------------

    [Fact]
    public async Task DepositAsync_CuentaActiva_AcreditaYAsociaAlCajero()
    {
        // Arrange
        var cuenta = Cuenta(1, "111111111", "cli-1", 100m);
        var service = CreateService();

        // Act
        var resultado = await service.DepositAsync("111111111", 400m, "cajero-1");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(500m, cuenta.Balance);
        var deposito = Assert.Single(_transactions);
        Assert.Equal(TransactionType.Credito, deposito.Type);
        Assert.Equal(AppConstants.DepositOrigin, deposito.Origin);
        Assert.Equal("111111111", deposito.Beneficiary);
        Assert.Equal("cajero-1", deposito.CashierId);
    }

    [Fact]
    public async Task WithdrawAsync_SaldoInsuficiente_RegistraRechazadaSinTocarBalance()
    {
        // Arrange
        var cuenta = Cuenta(1, "111111111", "cli-1", 100m);
        var service = CreateService();

        // Act
        var resultado = await service.WithdrawAsync("111111111", 500m, "cajero-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.RetiroExcedeSaldo, resultado.Message);
        Assert.Equal(100m, cuenta.Balance);

        var rechazada = Assert.Single(_transactions);
        Assert.Equal(TransactionStatus.Rechazada, rechazada.Status);
        Assert.Equal(AppConstants.WithdrawalBeneficiary, rechazada.Beneficiary);
        Assert.Equal("cajero-1", rechazada.CashierId);
    }

    [Fact]
    public async Task WithdrawAsync_ConSaldo_DebitaYRegistraRetiro()
    {
        // Arrange
        var cuenta = Cuenta(1, "111111111", "cli-1", 1_000m);
        var service = CreateService();

        // Act
        var resultado = await service.WithdrawAsync("111111111", 400m, "cajero-1");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(600m, cuenta.Balance);
        var retiro = Assert.Single(_transactions);
        Assert.Equal(TransactionType.Debito, retiro.Type);
        Assert.Equal(AppConstants.WithdrawalBeneficiary, retiro.Beneficiary);
        Assert.Equal(TransactionStatus.Aprobada, retiro.Status);
    }

    // ---------------- Transferencia entre cuentas propias ----------------

    [Fact]
    public async Task TransferBetweenOwnAccountsAsync_MenosDeDosCuentasActivas_Falla()
    {
        // Arrange
        Cuenta(1, "111111111", "cli-1", 1_000m);
        var service = CreateService();

        // Act
        var resultado = await service.TransferBetweenOwnAccountsAsync("cli-1", "111111111", "222222222", 100m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.RequiereDosCuentas, resultado.Message);
    }

    [Fact]
    public async Task TransferBetweenOwnAccountsAsync_ConFondos_MueveElBalanceEntreCuentasPropias()
    {
        // Arrange
        var origen = Cuenta(1, "111111111", "cli-1", 1_000m);
        var destino = Cuenta(2, "222222222", "cli-1", 0m);
        var service = CreateService();

        // Act
        var resultado = await service.TransferBetweenOwnAccountsAsync("cli-1", "111111111", "222222222", 250m);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(750m, origen.Balance);
        Assert.Equal(250m, destino.Balance);
        Assert.Equal(2, _transactions.Count);
    }

    // ---------------- Confirmación previa (anti-sobrepago visible) ----------------

    [Fact]
    public async Task PrepareCreditCardPaymentAsync_MuestraMontoIngresadoYMontoEfectivo()
    {
        // Arrange: deuda 500, pago 1,000 → la confirmación muestra ambos montos.
        Cuenta(1, "111111111", "cli-1", 2_000m);
        _cards.Add(new CreditCard
        {
            Id = 1, CardNumber = "5425123456781234", UserId = "cli-1", AdminUserId = "a",
            CreditLimit = 10_000m, OwedAmount = 500m, CvcHash = "h",
            Status = ProductStatus.Activa, ExpirationDate = DateTime.Now.AddYears(3)
        });
        var service = CreateService();

        // Act
        var resultado = await service.PrepareCreditCardPaymentAsync("111111111", "5425123456781234", 1_000m, ownerUserId: "cli-1");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(1_000m, resultado.Data!.RequestedAmount);
        Assert.Equal(500m, resultado.Data.EffectiveAmount);
        Assert.Equal("1234", resultado.Data.ProductReference);
    }
}
