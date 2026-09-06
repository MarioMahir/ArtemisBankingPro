using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using Moq;

namespace ArtemisBankingPro.Tests.Unit.Services;

public class DashboardServiceTests
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<ILoanService> _loanService = new();

    private readonly List<Transaction> _transactions = [];
    private readonly List<SavingsAccount> _accounts = [];
    private readonly List<Loan> _loans = [];
    private readonly List<CreditCard> _cards = [];

    private DashboardService CreateService()
    {
        _loanService.Setup(s => s.GetAverageDebtAsync()).ReturnsAsync(0m);
        return new DashboardService(
            MockHelpers.Repo(_transactions).Object,
            MockHelpers.Repo(_accounts).Object,
            MockHelpers.Repo(_loans).Object,
            MockHelpers.Repo(_cards).Object,
            _accountService.Object,
            _loanService.Object);
    }

    private static Transaction Tx(bool esPago, DateTime fecha, string origen = "111111111",
        string beneficiario = "222222222", string? cajero = null) => new()
    {
        Amount = 100m, Type = TransactionType.Debito, Origin = origen,
        Beneficiary = beneficiario, Status = TransactionStatus.Aprobada,
        IsPayment = esPago, CashierId = cajero, CreatedAt = fecha
    };

    [Fact]
    public async Task GetAdminDashboardAsync_ContadorDePagos_SoloIncluyePagosDeTarjetaYPrestamo()
    {
        // Arrange: 5 transacciones, solo 2 marcadas como pago (tarjeta/préstamo).
        _transactions.AddRange(
        [
            Tx(esPago: false, DateTime.Now),                       // transferencia
            Tx(esPago: false, DateTime.Now, origen: "DEPÓSITO"),   // depósito
            Tx(esPago: true, DateTime.Now),                        // pago tarjeta
            Tx(esPago: true, DateTime.Now.AddDays(-3)),            // pago préstamo (histórico)
            Tx(esPago: false, DateTime.Now.AddDays(-1))            // retiro
        ]);
        var service = CreateService();

        // Act
        var dashboard = await service.GetAdminDashboardAsync();

        // Assert
        Assert.Equal(5, dashboard.TotalTransactions);
        Assert.Equal(2, dashboard.TotalPayments);   // SOLO pagos
        Assert.Equal(3, dashboard.TransactionsToday);
        Assert.Equal(1, dashboard.PaymentsToday);
    }

    [Fact]
    public async Task GetAdminDashboardAsync_ProductosFinancieros_SumaCuentasPrestamosYTarjetasActivos()
    {
        // Arrange
        _accounts.AddRange(
        [
            new SavingsAccount { Id = 1, AccountNumber = "1", UserId = "u", Status = ProductStatus.Activa },
            new SavingsAccount { Id = 2, AccountNumber = "2", UserId = "u", Status = ProductStatus.Cancelada }
        ]);
        _loans.AddRange(
        [
            new Loan { Id = 1, LoanNumber = "3", UserId = "u", AdminUserId = "a", Status = LoanStatus.Activo },
            new Loan { Id = 2, LoanNumber = "4", UserId = "u", AdminUserId = "a", Status = LoanStatus.Completado }
        ]);
        _cards.Add(new CreditCard
        {
            Id = 1, CardNumber = "5", UserId = "u", AdminUserId = "a", CvcHash = "h",
            Status = ProductStatus.Activa
        });
        var service = CreateService();

        // Act
        var dashboard = await service.GetAdminDashboardAsync();

        // Assert: 1 cuenta activa + 1 préstamo activo + 1 tarjeta activa = 3.
        Assert.Equal(3, dashboard.TotalActiveProducts);
        Assert.Equal(1, dashboard.ActiveSavingsAccounts);
        Assert.Equal(1, dashboard.ActiveLoans);
        Assert.Equal(1, dashboard.ActiveCreditCards);
    }

    [Fact]
    public async Task GetCashierDashboardAsync_SoloCuentaOperacionesDelCajeroYDeHoy()
    {
        // Arrange
        _transactions.AddRange(
        [
            Tx(esPago: false, DateTime.Now, origen: AppConstants.DepositOrigin, cajero: "caj-1"),  // depósito hoy caj-1
            Tx(esPago: false, DateTime.Now, beneficiario: AppConstants.WithdrawalBeneficiary, cajero: "caj-1"), // retiro hoy caj-1
            Tx(esPago: true, DateTime.Now, cajero: "caj-1"),                    // pago hoy caj-1
            Tx(esPago: true, DateTime.Now, cajero: "caj-2"),                    // otro cajero
            Tx(esPago: true, DateTime.Now.AddDays(-1), cajero: "caj-1"),        // ayer
            Tx(esPago: false, DateTime.Now)                                     // sin cajero (cliente)
        ]);
        var service = CreateService();

        // Act
        var dashboard = await service.GetCashierDashboardAsync("caj-1");

        // Assert
        Assert.Equal(3, dashboard.TransactionsToday);
        Assert.Equal(1, dashboard.PaymentsToday);
        Assert.Equal(1, dashboard.DepositsToday);
        Assert.Equal(1, dashboard.WithdrawalsToday);
    }
}
