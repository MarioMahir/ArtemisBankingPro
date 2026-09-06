using ArtemisBankingPro.Infrastructure.Persistence.Repositories;
using ArtemisBankingPro.Tests.Integration.Common;

namespace ArtemisBankingPro.Tests.Integration.Repositories;

public class ProductNumberRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AccountOrLoanNumberExistsAsync_NumeroUsadoPorUnaCuenta_DevuelveTrue()
    {
        // Arrange
        Context.SavingsAccounts.Add(NuevaCuenta("123456789"));
        await Context.SaveChangesAsync();
        var repo = new ProductNumberRepository(Context);

        // Act + Assert: el espacio de numeración es COMPARTIDO.
        Assert.True(await repo.AccountOrLoanNumberExistsAsync("123456789"));
    }

    [Fact]
    public async Task AccountOrLoanNumberExistsAsync_NumeroUsadoPorUnPrestamo_DevuelveTrue()
    {
        // Arrange
        Context.Loans.Add(NuevoPrestamo("987654321"));
        await Context.SaveChangesAsync();
        var repo = new ProductNumberRepository(Context);

        // Act + Assert: un número de préstamo también bloquea el espacio de cuentas.
        Assert.True(await repo.AccountOrLoanNumberExistsAsync("987654321"));
    }

    [Fact]
    public async Task AccountOrLoanNumberExistsAsync_NumeroLibreEnAmbasTablas_DevuelveFalse()
    {
        // Arrange
        Context.SavingsAccounts.Add(NuevaCuenta("111111111"));
        Context.Loans.Add(NuevoPrestamo("222222222"));
        await Context.SaveChangesAsync();
        var repo = new ProductNumberRepository(Context);

        // Act + Assert
        Assert.False(await repo.AccountOrLoanNumberExistsAsync("333333333"));
    }

    [Fact]
    public async Task CardNumberExistsAsync_NumeroDeTarjetaExistente_DevuelveTrue()
    {
        // Arrange
        Context.CreditCards.Add(NuevaTarjeta("5425123456781234"));
        await Context.SaveChangesAsync();
        var repo = new ProductNumberRepository(Context);

        // Act + Assert
        Assert.True(await repo.CardNumberExistsAsync("5425123456781234"));
        Assert.False(await repo.CardNumberExistsAsync("9999999999999999"));
    }

    [Fact]
    public async Task CardNumberExistsAsync_LasTarjetasNoBloqueanElEspacioDeCuentasYPrestamos()
    {
        // Arrange: las tarjetas (16 dígitos) tienen su propio espacio de numeración.
        Context.CreditCards.Add(NuevaTarjeta("5425123456781234"));
        await Context.SaveChangesAsync();
        var repo = new ProductNumberRepository(Context);

        // Act + Assert
        Assert.False(await repo.AccountOrLoanNumberExistsAsync("5425123456781234"));
    }
}
