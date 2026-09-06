using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Tests.Integration.Common;

/// <summary>
/// Base para pruebas de integración sobre SQLite EN MEMORIA (sin base de datos real).
/// La conexión :memory: se mantiene abierta durante toda la prueba; cerrar la conexión
/// destruye la base. Cada prueba obtiene su propia base aislada.
/// </summary>
public abstract class SqliteTestBase : IDisposable
{
    protected readonly SqliteConnection Connection;
    protected readonly AppDbContext Context;

    protected SqliteTestBase()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();
        Context = CreateContext();
        Context.Database.EnsureCreated();
    }

    /// <summary>Contexto adicional sobre la MISMA base en memoria (para verificar lo persistido).</summary>
    protected AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(Connection)
            .Options);

    public void Dispose()
    {
        Context.Dispose();
        Connection.Dispose();
        GC.SuppressFinalize(this);
    }

    // ---------------- Constructores de entidades de prueba ----------------

    protected static SavingsAccount NuevaCuenta(
        string numero = "111111111", string userId = "cli-1",
        AccountType tipo = AccountType.Principal, decimal balance = 0m,
        ProductStatus estado = ProductStatus.Activa) => new()
    {
        AccountNumber = numero,
        UserId = userId,
        Type = tipo,
        Balance = balance,
        Status = estado,
        CreatedAt = DateTime.Now
    };

    protected static Loan NuevoPrestamo(
        string numero = "900000001", string userId = "cli-1", decimal monto = 100_000m) => new()
    {
        LoanNumber = numero,
        UserId = userId,
        AdminUserId = "admin-1",
        ApprovedAmount = monto,
        TermMonths = 12,
        AnnualInterestRate = 12m,
        Status = LoanStatus.Activo,
        CreatedAt = DateTime.Now
    };

    protected static CreditCard NuevaTarjeta(
        string numero = "5425123456781234", string userId = "cli-1") => new()
    {
        CardNumber = numero,
        UserId = userId,
        AdminUserId = "admin-1",
        CreditLimit = 10_000m,
        OwedAmount = 0m,
        ExpirationDate = DateTime.Now.AddYears(3),
        CvcHash = "abc123hash",
        Status = ProductStatus.Activa,
        CreatedAt = DateTime.Now
    };

    protected static Commerce NuevoComercio(
        string rnc = "101000001", string email = "comercio@test.com") => new()
    {
        Name = "Colmado Central",
        Email = email,
        PhoneNumber = "8095551234",
        Rnc = rnc,
        IsActive = true,
        CreatedAt = DateTime.Now
    };
}
