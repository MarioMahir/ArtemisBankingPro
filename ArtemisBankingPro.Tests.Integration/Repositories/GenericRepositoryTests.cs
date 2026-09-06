using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Infrastructure.Persistence.Repositories;
using ArtemisBankingPro.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Tests.Integration.Repositories;

public class GenericRepositoryTests : SqliteTestBase
{
    // ---------------- CRUD sobre SavingsAccounts ----------------

    [Fact]
    public async Task AddAsync_CuentaDeAhorro_SePersisteConSusDatos()
    {
        // Arrange
        var repo = new GenericRepository<SavingsAccount>(Context);
        var uow = new UnitOfWork(Context);

        // Act
        await repo.AddAsync(NuevaCuenta("123456789", balance: 500m));
        await uow.SaveChangesAsync();

        // Assert: verificado con un contexto NUEVO sobre la misma base.
        using var verificacion = CreateContext();
        var guardada = await verificacion.SavingsAccounts.SingleAsync();
        Assert.Equal("123456789", guardada.AccountNumber);
        Assert.Equal(500m, guardada.Balance);
        Assert.Equal(AccountType.Principal, guardada.Type);
        Assert.Equal(ProductStatus.Activa, guardada.Status);
    }

    [Fact]
    public async Task GetByIdAsync_CuentaExistente_LaDevuelve()
    {
        // Arrange
        var repo = new GenericRepository<SavingsAccount>(Context);
        var cuenta = NuevaCuenta();
        await repo.AddAsync(cuenta);
        await new UnitOfWork(Context).SaveChangesAsync();

        // Act
        var encontrada = await repo.GetByIdAsync(cuenta.Id);

        // Assert
        Assert.NotNull(encontrada);
        Assert.Equal(cuenta.AccountNumber, encontrada!.AccountNumber);
    }

    [Fact]
    public async Task Update_CambioDeBalance_SePersiste()
    {
        // Arrange
        var repo = new GenericRepository<SavingsAccount>(Context);
        var uow = new UnitOfWork(Context);
        var cuenta = NuevaCuenta(balance: 100m);
        await repo.AddAsync(cuenta);
        await uow.SaveChangesAsync();

        // Act
        cuenta.Balance = 750m;
        repo.Update(cuenta);
        await uow.SaveChangesAsync();

        // Assert
        using var verificacion = CreateContext();
        Assert.Equal(750m, (await verificacion.SavingsAccounts.SingleAsync()).Balance);
    }

    [Fact]
    public async Task Delete_EntidadExistente_DesapareceDeLaBase()
    {
        // Arrange
        var repo = new GenericRepository<SavingsAccount>(Context);
        var uow = new UnitOfWork(Context);
        var cuenta = NuevaCuenta();
        await repo.AddAsync(cuenta);
        await uow.SaveChangesAsync();

        // Act
        repo.Delete(cuenta);
        await uow.SaveChangesAsync();

        // Assert
        Assert.Empty(await repo.GetAllAsync());
    }

    [Fact]
    public async Task Query_PermiteComponerFiltrosSobreLaBase()
    {
        // Arrange
        var repo = new GenericRepository<SavingsAccount>(Context);
        var uow = new UnitOfWork(Context);
        await repo.AddAsync(NuevaCuenta("100000001", "cli-1", AccountType.Principal));
        await repo.AddAsync(NuevaCuenta("100000002", "cli-1", AccountType.Secundaria));
        await repo.AddAsync(NuevaCuenta("100000003", "cli-2", AccountType.Principal));
        await uow.SaveChangesAsync();

        // Act
        var deCli1 = await repo.Query().Where(a => a.UserId == "cli-1").ToListAsync();
        var principales = await repo.Query().CountAsync(a => a.Type == AccountType.Principal);

        // Assert
        Assert.Equal(2, deCli1.Count);
        Assert.Equal(2, principales);
    }

    [Fact]
    public async Task Query_NumeroDeCuentaUnico_DuplicadoLanzaExcepcion()
    {
        // Arrange
        var repo = new GenericRepository<SavingsAccount>(Context);
        var uow = new UnitOfWork(Context);
        await repo.AddAsync(NuevaCuenta("123456789"));
        await uow.SaveChangesAsync();

        // Act + Assert: índice único sobre AccountNumber.
        await repo.AddAsync(NuevaCuenta("123456789", "otro-cliente"));
        await Assert.ThrowsAsync<DbUpdateException>(() => uow.SaveChangesAsync());
    }

    // ---------------- Loans + cascada de Installments ----------------

    [Fact]
    public async Task AddAsync_PrestamoConCuotas_PersisteLaTablaCompleta()
    {
        // Arrange
        var repo = new GenericRepository<Loan>(Context);
        var uow = new UnitOfWork(Context);
        var prestamo = NuevoPrestamo();
        for (var i = 1; i <= 12; i++)
        {
            prestamo.Installments.Add(new LoanInstallment
            {
                Number = i,
                DueDate = DateTime.Today.AddMonths(i),
                Value = 8_884.88m,
                InterestAmount = 100m,
                PrincipalAmount = 8_784.88m,
                PendingAmount = 8_884.88m,
                Status = InstallmentStatus.Pendiente
            });
        }

        // Act
        await repo.AddAsync(prestamo);
        await uow.SaveChangesAsync();

        // Assert
        using var verificacion = CreateContext();
        var guardado = await verificacion.Loans.Include(l => l.Installments).SingleAsync();
        Assert.Equal(12, guardado.Installments.Count);
        Assert.Equal("900000001", guardado.LoanNumber);
    }

    [Fact]
    public async Task Delete_Prestamo_EliminaSusCuotasEnCascada()
    {
        // Arrange
        var repo = new GenericRepository<Loan>(Context);
        var uow = new UnitOfWork(Context);
        var prestamo = NuevoPrestamo();
        prestamo.Installments.Add(new LoanInstallment
        {
            Number = 1, DueDate = DateTime.Today.AddMonths(1), Value = 100m,
            PendingAmount = 100m, Status = InstallmentStatus.Pendiente
        });
        await repo.AddAsync(prestamo);
        await uow.SaveChangesAsync();

        // Act
        repo.Delete(prestamo);
        await uow.SaveChangesAsync();

        // Assert: la cascada configurada elimina las cuotas huérfanas.
        using var verificacion = CreateContext();
        Assert.Empty(await verificacion.Loans.ToListAsync());
        Assert.Empty(await verificacion.LoanInstallments.ToListAsync());
    }

    [Fact]
    public async Task Query_NumeroDePrestamoUnico_DuplicadoLanzaExcepcion()
    {
        // Arrange
        var repo = new GenericRepository<Loan>(Context);
        var uow = new UnitOfWork(Context);
        await repo.AddAsync(NuevoPrestamo("900000001"));
        await uow.SaveChangesAsync();

        // Act + Assert
        await repo.AddAsync(NuevoPrestamo("900000001", "cli-2"));
        await Assert.ThrowsAsync<DbUpdateException>(() => uow.SaveChangesAsync());
    }

    // ---------------- CreditCards + Consumptions ----------------

    [Fact]
    public async Task AddAsync_TarjetaConConsumos_PersisteLaRelacion()
    {
        // Arrange
        var repo = new GenericRepository<CreditCard>(Context);
        var uow = new UnitOfWork(Context);
        var tarjeta = NuevaTarjeta();
        tarjeta.Consumptions.Add(new CardConsumption
        {
            CommerceName = "AVANCE", Amount = 106.25m,
            Status = ConsumptionStatus.Aprobado, CreatedAt = DateTime.Now
        });
        tarjeta.Consumptions.Add(new CardConsumption
        {
            CommerceName = "Colmado Central", Amount = 300m,
            Status = ConsumptionStatus.Rechazado, CreatedAt = DateTime.Now
        });

        // Act
        await repo.AddAsync(tarjeta);
        await uow.SaveChangesAsync();

        // Assert
        using var verificacion = CreateContext();
        var guardada = await verificacion.CreditCards.Include(c => c.Consumptions).SingleAsync();
        Assert.Equal(2, guardada.Consumptions.Count);
        Assert.Contains(guardada.Consumptions, c => c.Status == ConsumptionStatus.Rechazado);
    }

    [Fact]
    public async Task Query_NumeroDeTarjetaUnico_DuplicadoLanzaExcepcion()
    {
        // Arrange
        var repo = new GenericRepository<CreditCard>(Context);
        var uow = new UnitOfWork(Context);
        await repo.AddAsync(NuevaTarjeta("5425123456781234"));
        await uow.SaveChangesAsync();

        // Act + Assert
        await repo.AddAsync(NuevaTarjeta("5425123456781234", "cli-2"));
        await Assert.ThrowsAsync<DbUpdateException>(() => uow.SaveChangesAsync());
    }

    // ---------------- Beneficiaries (índice único UserId + SavingsAccountId) ----------------

    [Fact]
    public async Task AddAsync_BeneficiarioDuplicado_LanzaPorIndiceUnico()
    {
        // Arrange: misma cuenta beneficiaria registrada dos veces por el mismo cliente.
        var cuentaRepo = new GenericRepository<SavingsAccount>(Context);
        var beneficiarioRepo = new GenericRepository<Beneficiary>(Context);
        var uow = new UnitOfWork(Context);

        var cuenta = NuevaCuenta("222222222", "otro-cliente");
        await cuentaRepo.AddAsync(cuenta);
        await uow.SaveChangesAsync();

        await beneficiarioRepo.AddAsync(new Beneficiary
        {
            UserId = "cli-1", SavingsAccountId = cuenta.Id, CreatedAt = DateTime.Now
        });
        await uow.SaveChangesAsync();

        // Act + Assert
        await beneficiarioRepo.AddAsync(new Beneficiary
        {
            UserId = "cli-1", SavingsAccountId = cuenta.Id, CreatedAt = DateTime.Now
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => uow.SaveChangesAsync());
    }

    [Fact]
    public async Task AddAsync_MismaCuentaParaOtroCliente_SiSePermite()
    {
        // Arrange: el índice único es por (UserId, SavingsAccountId), no global.
        var cuentaRepo = new GenericRepository<SavingsAccount>(Context);
        var beneficiarioRepo = new GenericRepository<Beneficiary>(Context);
        var uow = new UnitOfWork(Context);

        var cuenta = NuevaCuenta("222222222", "dueno");
        await cuentaRepo.AddAsync(cuenta);
        await uow.SaveChangesAsync();

        // Act
        await beneficiarioRepo.AddAsync(new Beneficiary { UserId = "cli-1", SavingsAccountId = cuenta.Id });
        await beneficiarioRepo.AddAsync(new Beneficiary { UserId = "cli-2", SavingsAccountId = cuenta.Id });
        await uow.SaveChangesAsync();

        // Assert
        Assert.Equal(2, await beneficiarioRepo.Query().CountAsync());
    }

    // ---------------- Commerces (RNC y correo únicos) ----------------

    [Fact]
    public async Task AddAsync_ComercioConRncDuplicado_LanzaExcepcion()
    {
        // Arrange
        var repo = new GenericRepository<Commerce>(Context);
        var uow = new UnitOfWork(Context);
        await repo.AddAsync(NuevoComercio(rnc: "101000001", email: "a@test.com"));
        await uow.SaveChangesAsync();

        // Act + Assert
        await repo.AddAsync(NuevoComercio(rnc: "101000001", email: "b@test.com"));
        await Assert.ThrowsAsync<DbUpdateException>(() => uow.SaveChangesAsync());
    }

    [Fact]
    public async Task AddAsync_ComercioConCorreoDuplicado_LanzaExcepcion()
    {
        // Arrange
        var repo = new GenericRepository<Commerce>(Context);
        var uow = new UnitOfWork(Context);
        await repo.AddAsync(NuevoComercio(rnc: "101000001", email: "mismo@test.com"));
        await uow.SaveChangesAsync();

        // Act + Assert
        await repo.AddAsync(NuevoComercio(rnc: "101000002", email: "mismo@test.com"));
        await Assert.ThrowsAsync<DbUpdateException>(() => uow.SaveChangesAsync());
    }

    // ---------------- Transactions (persistencia y orden por fecha) ----------------

    [Fact]
    public async Task Query_Transacciones_SeOrdenanDeLaMasRecienteALaMasAntigua()
    {
        // Arrange
        var cuentaRepo = new GenericRepository<SavingsAccount>(Context);
        var txRepo = new GenericRepository<Transaction>(Context);
        var uow = new UnitOfWork(Context);

        var cuenta = NuevaCuenta();
        await cuentaRepo.AddAsync(cuenta);
        await uow.SaveChangesAsync();

        var baseDate = new DateTime(2026, 8, 1, 10, 0, 0);
        for (var i = 0; i < 3; i++)
        {
            await txRepo.AddAsync(new Transaction
            {
                AccountId = cuenta.Id, Amount = 100m + i, Type = TransactionType.Credito,
                Beneficiary = cuenta.AccountNumber, Origin = "DEPÓSITO",
                Status = TransactionStatus.Aprobada, CreatedAt = baseDate.AddDays(i)
            });
        }
        await uow.SaveChangesAsync();

        // Act: más recientes primero (regla de todos los listados).
        var ordenadas = await txRepo.Query()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        // Assert
        Assert.Equal(3, ordenadas.Count);
        Assert.Equal(102m, ordenadas[0].Amount); // la de fecha más reciente
        Assert.Equal(100m, ordenadas[2].Amount);
    }

    [Fact]
    public async Task AddAsync_TransaccionRechazada_ConservaEstadoTipoYReferencias()
    {
        // Arrange
        var cuentaRepo = new GenericRepository<SavingsAccount>(Context);
        var txRepo = new GenericRepository<Transaction>(Context);
        var uow = new UnitOfWork(Context);
        var cuenta = NuevaCuenta();
        await cuentaRepo.AddAsync(cuenta);
        await uow.SaveChangesAsync();

        // Act
        await txRepo.AddAsync(new Transaction
        {
            AccountId = cuenta.Id, Amount = 999m, Type = TransactionType.Debito,
            Beneficiary = "RETIRO", Origin = cuenta.AccountNumber,
            Status = TransactionStatus.Rechazada, CashierId = "cajero-1",
            IsPayment = false, CreatedAt = DateTime.Now
        });
        await uow.SaveChangesAsync();

        // Assert
        using var verificacion = CreateContext();
        var guardada = await verificacion.Transactions.SingleAsync();
        Assert.Equal(TransactionStatus.Rechazada, guardada.Status);
        Assert.Equal(TransactionType.Debito, guardada.Type);
        Assert.Equal("RETIRO", guardada.Beneficiary);
        Assert.Equal("cajero-1", guardada.CashierId);
    }
}
