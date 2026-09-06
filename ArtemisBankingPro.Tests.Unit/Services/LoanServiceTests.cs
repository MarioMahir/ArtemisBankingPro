using ArtemisBankingPro.Core.Application.Dtos.Email;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using Moq;

namespace ArtemisBankingPro.Tests.Unit.Services;

public class LoanServiceTests
{
    private readonly Mock<IProductNumberGenerator> _numberGenerator = new();
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<IEmailService> _emailService = new();

    private readonly List<Loan> _loans = [];
    private readonly List<LoanInstallment> _installments = [];
    private readonly List<SavingsAccount> _accounts = [];
    private readonly List<Transaction> _transactions = [];
    private readonly List<CreditCard> _cards = [];

    private LoanService CreateService()
    {
        _numberGenerator.Setup(g => g.GenerateAccountOrLoanNumberAsync()).ReturnsAsync("900000001");
        _emailService.Setup(e => e.SendAsync(It.IsAny<EmailRequest>())).ReturnsAsync(true);
        _accountService.Setup(a => a.GetByIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync([]);
        return new LoanService(
            MockHelpers.Repo(_loans).Object,
            MockHelpers.Repo(_installments).Object,
            MockHelpers.Repo(_accounts).Object,
            MockHelpers.Repo(_transactions).Object,
            MockHelpers.Repo(_cards).Object,
            _numberGenerator.Object,
            _accountService.Object,
            _emailService.Object,
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

    private static CreateLoanRequest Solicitud(
        string clientId = "cli-1", decimal monto = 100_000m, int plazo = 12, decimal tasa = 12m) => new()
    {
        ClientId = clientId,
        CapitalAmount = monto,
        TermInMonths = plazo,
        AnnualInterestRate = tasa
    };

    // ---------------- Elegibilidad: 1 préstamo activo por cliente ----------------

    [Fact]
    public async Task GetEligibleClientsAsync_ClienteConPrestamoActivo_QuedaExcluido()
    {
        // Arrange
        _accountService.Setup(a => a.GetActiveClientsAsync())
            .ReturnsAsync([Cliente("cli-1"), Cliente("cli-2")]);
        _loans.Add(new Loan
        {
            Id = 1, LoanNumber = "900000009", UserId = "cli-1", AdminUserId = "admin-1",
            ApprovedAmount = 50_000m, TermMonths = 12, Status = LoanStatus.Activo
        });
        var service = CreateService();

        // Act
        var elegibles = await service.GetEligibleClientsAsync();

        // Assert: solo cli-2 es elegible.
        var unico = Assert.Single(elegibles);
        Assert.Equal("cli-2", unico.UserId);
    }

    [Fact]
    public async Task GetEligibleClientsAsync_ClienteConPrestamoCompletado_SigueSiendoElegible()
    {
        // Arrange
        _accountService.Setup(a => a.GetActiveClientsAsync()).ReturnsAsync([Cliente("cli-1")]);
        _loans.Add(new Loan
        {
            Id = 1, LoanNumber = "900000009", UserId = "cli-1", AdminUserId = "admin-1",
            ApprovedAmount = 50_000m, TermMonths = 12, Status = LoanStatus.Completado
        });
        var service = CreateService();

        // Act
        var elegibles = await service.GetEligibleClientsAsync();

        // Assert
        Assert.Single(elegibles);
    }

    [Fact]
    public async Task CreateLoanAsync_ClienteConPrestamoActivo_FallaSinCrearNada()
    {
        // Arrange
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        _loans.Add(new Loan
        {
            Id = 1, LoanNumber = "900000009", UserId = "cli-1", AdminUserId = "admin-1",
            ApprovedAmount = 50_000m, TermMonths = 12, Status = LoanStatus.Activo
        });
        var service = CreateService();

        // Act
        var resultado = await service.CreateLoanAsync(Solicitud(), "admin-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.ClienteConPrestamoActivo, resultado.Message);
        Assert.Single(_loans); // no se agregó otro préstamo
        Assert.Empty(_transactions);
    }

    // ---------------- Desembolso requiere principal activa ----------------

    [Fact]
    public async Task CreateLoanAsync_ClienteSinCuentaPrincipalActiva_NoDesembolsa()
    {
        // Arrange: cliente válido pero sin cuenta principal activa.
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var service = CreateService();

        // Act
        var resultado = await service.CreateLoanAsync(Solicitud(), "admin-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.ClienteSinCuentaPrincipal, resultado.Message);
        Assert.Empty(_loans);
        Assert.Empty(_installments);
        Assert.Empty(_transactions);
    }

    [Fact]
    public async Task CreateLoanAsync_ClienteValido_DesembolsaALaPrincipalConTransaccionCredito()
    {
        // Arrange
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var principal = new SavingsAccount
        {
            Id = 10, AccountNumber = "111111111", UserId = "cli-1",
            Type = AccountType.Principal, Status = ProductStatus.Activa, Balance = 5_000m
        };
        _accounts.Add(principal);
        var service = CreateService();

        // Act
        var resultado = await service.CreateLoanAsync(Solicitud(monto: 100_000m, plazo: 12, tasa: 12m), "admin-1");

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(105_000m, principal.Balance); // 5,000 + 100,000

        var prestamo = Assert.Single(_loans);
        Assert.Equal("900000001", prestamo.LoanNumber);
        Assert.Equal(LoanStatus.Activo, prestamo.Status);
        Assert.Equal(12, _installments.Count);
        Assert.All(_installments, i => Assert.Equal(InstallmentStatus.Pendiente, i.Status));
        Assert.All(_installments, i => Assert.False(i.IsOverdue));
        Assert.Equal(106_618.56m, _installments.Sum(i => i.PendingAmount));

        var credito = Assert.Single(_transactions);
        Assert.Equal(TransactionType.Credito, credito.Type);
        Assert.Equal(100_000m, credito.Amount);
        Assert.Equal("900000001", credito.Origin); // origen = nº de préstamo
        Assert.Equal(TransactionStatus.Aprobada, credito.Status);
    }

    [Fact]
    public async Task CreateLoanAsync_FalloDeCorreo_NoRevierteYAvisaConMensaje()
    {
        // Arrange
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        _accounts.Add(new SavingsAccount
        {
            Id = 10, AccountNumber = "111111111", UserId = "cli-1",
            Type = AccountType.Principal, Status = ProductStatus.Activa
        });
        var service = CreateService();
        _emailService.Setup(e => e.SendAsync(It.IsAny<EmailRequest>())).ReturnsAsync(false);

        // Act
        var resultado = await service.CreateLoanAsync(Solicitud(), "admin-1");

        // Assert: el préstamo se crea igual (el fallo de correo NUNCA revierte).
        Assert.True(resultado.Succeeded);
        Assert.Equal(Mensajes.PrestamoCreadoCorreoFallido, resultado.Message);
        Assert.Single(_loans);
    }

    [Theory]
    [InlineData(7)]   // no múltiplo de 6
    [InlineData(0)]
    [InlineData(66)]  // fuera de rango
    public async Task CreateLoanAsync_PlazoInvalido_Falla(int plazo)
    {
        // Arrange
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var service = CreateService();

        // Act
        var resultado = await service.CreateLoanAsync(Solicitud(plazo: plazo), "admin-1");

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.PlazoInvalido, resultado.Message);
    }

    // ---------------- Evaluación de riesgo ----------------

    [Fact]
    public async Task EvaluateRiskAsync_DeudaActualSuperaElPromedio_DevuelveAltoRiesgoActual()
    {
        // Arrange: cli-1 debe 80,000 (tarjeta); cli-2 debe 0 → promedio 40,000.
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        _accountService.Setup(a => a.GetActiveClientsAsync())
            .ReturnsAsync([Cliente("cli-1"), Cliente("cli-2")]);
        _cards.Add(new CreditCard
        {
            Id = 1, CardNumber = "1111222233334444", UserId = "cli-1", AdminUserId = "a",
            CreditLimit = 100_000m, OwedAmount = 80_000m, CvcHash = "h", Status = ProductStatus.Activa
        });
        var service = CreateService();

        // Act
        var resultado = await service.EvaluateRiskAsync(Solicitud(monto: 10_000m, tasa: 0m, plazo: 6));

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(Core.Application.Helpers.RiskLevel.CurrentHighRisk, resultado.Data!.RiskLevel);
        Assert.Equal(Mensajes.ClienteAltoRiesgoActual, resultado.Data.WarningMessage);
        Assert.Equal(80_000m, resultado.Data.CurrentDebt);
        Assert.Equal(40_000m, resultado.Data.AverageDebt);
    }

    [Fact]
    public async Task GetAverageDebtAsync_SinClientesActivos_DevuelveCero()
    {
        // Arrange
        _accountService.Setup(a => a.GetActiveClientsAsync()).ReturnsAsync([]);
        var service = CreateService();

        // Act + Assert
        Assert.Equal(0m, await service.GetAverageDebtAsync());
    }

    // ---------------- Editar tasa ----------------

    private void AgregarPrestamoConCuotas()
    {
        _loans.Add(new Loan
        {
            Id = 1, LoanNumber = "900000001", UserId = "cli-1", AdminUserId = "admin-1",
            ApprovedAmount = 3_000m, TermMonths = 3, AnnualInterestRate = 0m, Status = LoanStatus.Activo
        });
    }

    [Fact]
    public async Task UpdateRateAsync_SinCuotasFuturasPendientes_Falla()
    {
        // Arrange: todas las cuotas están pagadas o vencidas.
        AgregarPrestamoConCuotas();
        _installments.AddRange(
        [
            new LoanInstallment
            {
                Id = 1, LoanId = 1, Number = 1, DueDate = DateTime.Today.AddMonths(-2),
                Value = 1_000m, PrincipalAmount = 1_000m, PendingAmount = 0m,
                Status = InstallmentStatus.Pagada
            },
            new LoanInstallment
            {
                Id = 2, LoanId = 1, Number = 2, DueDate = DateTime.Today.AddMonths(-1),
                Value = 1_000m, PrincipalAmount = 1_000m, PendingAmount = 1_000m,
                Status = InstallmentStatus.Pendiente, IsOverdue = true // vencida: DueDate ≤ hoy
            }
        ]);
        var service = CreateService();

        // Act
        var resultado = await service.UpdateRateAsync(1, 10m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.SinCuotasFuturas, resultado.Message);
    }

    [Fact]
    public async Task UpdateRateAsync_RecalculaSoloLasCuotasPendientesConVencimientoFuturo()
    {
        // Arrange: pagada + parcial + vencida + 2 futuras pendientes (tasa original 0%).
        AgregarPrestamoConCuotas();
        var pagada = new LoanInstallment
        {
            Id = 1, LoanId = 1, Number = 1, DueDate = DateTime.Today.AddMonths(-2),
            Value = 1_000m, PrincipalAmount = 1_000m, PendingAmount = 0m, Status = InstallmentStatus.Pagada
        };
        var parcial = new LoanInstallment
        {
            Id = 2, LoanId = 1, Number = 2, DueDate = DateTime.Today.AddMonths(-1),
            Value = 1_000m, PrincipalAmount = 1_000m, PendingAmount = 400m,
            Status = InstallmentStatus.ParcialmentePagada
        };
        var futura1 = new LoanInstallment
        {
            Id = 3, LoanId = 1, Number = 3, DueDate = DateTime.Today.AddMonths(1),
            Value = 1_000m, PrincipalAmount = 1_000m, PendingAmount = 1_000m,
            Status = InstallmentStatus.Pendiente
        };
        var futura2 = new LoanInstallment
        {
            Id = 4, LoanId = 1, Number = 4, DueDate = DateTime.Today.AddMonths(2),
            Value = 1_000m, PrincipalAmount = 1_000m, PendingAmount = 1_000m,
            Status = InstallmentStatus.Pendiente
        };
        _installments.AddRange([pagada, parcial, futura1, futura2]);
        _accountService.Setup(a => a.GetByIdAsync("cli-1")).ReturnsAsync(Cliente());
        var service = CreateService();

        // Act: nueva tasa 12% anual (1% mensual) sobre el capital restante de las futuras (2,000).
        var resultado = await service.UpdateRateAsync(1, 12m);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal(12m, _loans[0].AnnualInterestRate);

        // Pagada y parcial NO se tocan.
        Assert.Equal(1_000m, pagada.Value);
        Assert.Equal(0m, pagada.PendingAmount);
        Assert.Equal(1_000m, parcial.Value);
        Assert.Equal(400m, parcial.PendingAmount);

        // Las futuras se recalculan con interés (> 1,000) y su capital sigue sumando 2,000.
        Assert.True(futura1.InterestAmount > 0m);
        Assert.True(futura1.Value > 1_000m);
        Assert.Equal(futura1.Value, futura2.Value); // cuota fija
        Assert.Equal(2_000m, futura1.PrincipalAmount + futura2.PrincipalAmount);
        Assert.Equal(futura1.Value, futura1.PendingAmount);
    }

    [Fact]
    public async Task UpdateRateAsync_PrestamoCompletado_Falla()
    {
        // Arrange
        _loans.Add(new Loan
        {
            Id = 1, LoanNumber = "900000001", UserId = "cli-1", AdminUserId = "admin-1",
            ApprovedAmount = 3_000m, TermMonths = 3, Status = LoanStatus.Completado
        });
        var service = CreateService();

        // Act
        var resultado = await service.UpdateRateAsync(1, 10m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.SoloTasaPrestamosActivos, resultado.Message);
    }

    [Fact]
    public async Task UpdateRateAsync_TasaNegativa_Falla()
    {
        // Arrange
        AgregarPrestamoConCuotas();
        var service = CreateService();

        // Act
        var resultado = await service.UpdateRateAsync(1, -1m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.TasaNegativa, resultado.Message);
    }

    [Fact]
    public async Task UpdateRateAsync_PrestamoInexistente_Falla()
    {
        // Arrange
        var service = CreateService();

        // Act
        var resultado = await service.UpdateRateAsync(99, 10m);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.PrestamoNoExiste, resultado.Message);
    }
}
