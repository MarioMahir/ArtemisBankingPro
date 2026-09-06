using ArtemisBankingPro.Infrastructure.Persistence.Repositories;
using ArtemisBankingPro.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Tests.Integration.Repositories;

public class UnitOfWorkTests : SqliteTestBase
{
    [Fact]
    public async Task ExecuteInTransactionAsync_OperacionQueFallaAMitad_NoPersisteNada()
    {
        // Arrange
        var uow = new UnitOfWork(Context);

        // Act: se agrega y GUARDA una cuenta dentro de la transacción y luego falla.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteInTransactionAsync(async () =>
            {
                Context.SavingsAccounts.Add(NuevaCuenta("111111111"));
                await uow.SaveChangesAsync(); // guardado intermedio dentro de la transacción

                Context.SavingsAccounts.Add(NuevaCuenta("222222222"));
                throw new InvalidOperationException("Fallo simulado a mitad de la operación");
            }));

        // Assert: TODO se revirtió, incluido el guardado intermedio (todo o nada).
        using var verificacion = CreateContext();
        Assert.Empty(await verificacion.SavingsAccounts.ToListAsync());
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_OperacionExitosa_PersisteTodo()
    {
        // Arrange
        var uow = new UnitOfWork(Context);

        // Act: operación multi-entidad completa.
        await uow.ExecuteInTransactionAsync(async () =>
        {
            var cuenta = NuevaCuenta("111111111", balance: 500m);
            Context.SavingsAccounts.Add(cuenta);
            await uow.SaveChangesAsync();

            Context.Transactions.Add(new Core.Domain.Entities.Transaction
            {
                AccountId = cuenta.Id,
                Amount = 500m,
                Type = Core.Domain.Enums.TransactionType.Credito,
                Beneficiary = cuenta.AccountNumber,
                Origin = "APERTURA",
                Status = Core.Domain.Enums.TransactionStatus.Aprobada,
                CreatedAt = DateTime.Now
            });
        });

        // Assert: cuenta Y transacción quedaron persistidas.
        using var verificacion = CreateContext();
        Assert.Single(await verificacion.SavingsAccounts.ToListAsync());
        Assert.Single(await verificacion.Transactions.ToListAsync());
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ConTransaccionExterna_ReutilizaSinCommit()
    {
        // Arrange: ya existe una transacción activa (camino de reutilización).
        var uow = new UnitOfWork(Context);
        await using var externa = await Context.Database.BeginTransactionAsync();

        // Act: la operación interna se ejecuta y guarda, pero NO comitea la externa.
        await uow.ExecuteInTransactionAsync(async () =>
        {
            Context.SavingsAccounts.Add(NuevaCuenta("111111111"));
            await Task.CompletedTask;
        });

        // Assert: al revertir la externa no queda nada — la operación interna NO
        // comiteó por su cuenta; quien abrió la transacción decide el destino.
        await externa.RollbackAsync();
        using var despues = CreateContext();
        Assert.Empty(await despues.SavingsAccounts.ToListAsync());
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ConTransaccionExternaComiteada_PersisteAlConfirmar()
    {
        // Arrange
        var uow = new UnitOfWork(Context);
        await using (var externa = await Context.Database.BeginTransactionAsync())
        {
            // Act
            await uow.ExecuteInTransactionAsync(async () =>
            {
                Context.SavingsAccounts.Add(NuevaCuenta("111111111"));
                await Task.CompletedTask;
            });
            await externa.CommitAsync();
        }

        // Assert
        using var verificacion = CreateContext();
        Assert.Single(await verificacion.SavingsAccounts.ToListAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_DevuelveElNumeroDeFilasAfectadas()
    {
        // Arrange
        var uow = new UnitOfWork(Context);
        Context.SavingsAccounts.Add(NuevaCuenta("111111111"));
        Context.SavingsAccounts.Add(NuevaCuenta("222222222"));

        // Act
        var filas = await uow.SaveChangesAsync();

        // Assert
        Assert.Equal(2, filas);
    }
}
