using ArtemisBankingPro.Core.Application.Extensions;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Infrastructure.Persistence.Repositories;
using ArtemisBankingPro.Tests.Integration.Common;

namespace ArtemisBankingPro.Tests.Integration.Repositories;

/// <summary>
/// Verifica que los operadores async propios de Core.Application (sin dependencia de
/// EF Core) se traducen y ejecutan correctamente contra un proveedor EF real (SQLite),
/// incluyendo los agregados COUNT y SUM que usan GroupBy por constante.
/// </summary>
public class QueryableAsyncExtensionsTests : SqliteTestBase
{
    private async Task<GenericRepository<SavingsAccount>> RepoConCuentasAsync()
    {
        var repo = new GenericRepository<SavingsAccount>(Context);
        await repo.AddAsync(NuevaCuenta("100000001", "cli-1", AccountType.Principal, balance: 1_000.50m));
        await repo.AddAsync(NuevaCuenta("100000002", "cli-1", AccountType.Secundaria, balance: 250.25m));
        await repo.AddAsync(NuevaCuenta("100000003", "cli-2", AccountType.Principal, balance: 500.00m));
        await new UnitOfWork(Context).SaveChangesAsync();
        return repo;
    }

    [Fact]
    public async Task ToListAsync_SobreEF_MaterializaTodasLasFilas()
    {
        var repo = await RepoConCuentasAsync();

        var deCli1 = await repo.Query().Where(a => a.UserId == "cli-1").ToListAsync();

        Assert.Equal(2, deCli1.Count);
        Assert.All(deCli1, a => Assert.Equal("cli-1", a.UserId));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_SobreEF_EncuentraYDevuelveNullSiNoExiste()
    {
        var repo = await RepoConCuentasAsync();

        var existente = await repo.Query().FirstOrDefaultAsync(a => a.AccountNumber == "100000002");
        var inexistente = await repo.Query().FirstOrDefaultAsync(a => a.AccountNumber == "999999999");

        Assert.NotNull(existente);
        Assert.Equal(AccountType.Secundaria, existente!.Type);
        Assert.Null(inexistente);
    }

    [Fact]
    public async Task AnyAsync_SobreEF_DistingueExistenciaDeAusencia()
    {
        var repo = await RepoConCuentasAsync();

        Assert.True(await repo.Query().AnyAsync(a => a.UserId == "cli-2"));
        Assert.False(await repo.Query().AnyAsync(a => a.UserId == "cli-999"));
    }

    [Fact]
    public async Task CountAsync_SobreEF_CuentaConYSinFiltro()
    {
        var repo = await RepoConCuentasAsync();

        Assert.Equal(3, await repo.Query().CountAsync());
        Assert.Equal(2, await repo.Query().CountAsync(a => a.Type == AccountType.Principal));
        Assert.Equal(0, await repo.Query().Where(a => a.UserId == "cli-999").CountAsync());
    }

    [Fact]
    public async Task SumAsync_SobreEF_SumaBalancesYDevuelveCeroEnVacio()
    {
        var repo = await RepoConCuentasAsync();

        var total = await repo.Query().SumAsync(a => a.Balance);
        var vacio = await repo.Query().Where(a => a.UserId == "cli-999").SumAsync(a => a.Balance);

        Assert.Equal(1_750.75m, total);
        Assert.Equal(0m, vacio);
    }

    [Fact]
    public async Task Operadores_SobreProveedorEnMemoria_UsanElFallbackSincrono()
    {
        // Un IQueryable de lista en memoria no implementa IAsyncEnumerable:
        // los operadores deben resolverse igual con el fallback síncrono.
        var enMemoria = new List<SavingsAccount>
        {
            NuevaCuenta("100000001", balance: 100m),
            NuevaCuenta("100000002", balance: 200m)
        }.AsQueryable();

        Assert.Equal(2, (await enMemoria.ToListAsync()).Count);
        Assert.NotNull(await enMemoria.FirstOrDefaultAsync(a => a.AccountNumber == "100000002"));
        Assert.True(await enMemoria.AnyAsync());
        Assert.Equal(2, await enMemoria.CountAsync());
        Assert.Equal(300m, await enMemoria.SumAsync(a => a.Balance));
    }
}
