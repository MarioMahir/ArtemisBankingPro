using ArtemisBankingPro.Core.Application.Helpers;

namespace ArtemisBankingPro.Tests.Unit.Helpers;

public class AmortizationCalculatorTests
{
    // Ejemplo EXACTO del spec: 100,000 a 12 meses al 12% anual → cuota 8,884.88.
    [Fact]
    public void CalculateMonthlyInstallment_EjemploDelSpec_DevuelveCuota8884_88()
    {
        // Arrange
        decimal principal = 100_000m;
        decimal tasaAnual = 12m;
        int plazo = 12;

        // Act
        var cuota = AmortizationCalculator.CalculateMonthlyInstallment(principal, tasaAnual, plazo);

        // Assert
        Assert.Equal(8_884.88m, cuota);
    }

    [Fact]
    public void BuildSchedule_EjemploDelSpec_TotalAPagarEs106618_56()
    {
        // Arrange + Act
        var tabla = AmortizationCalculator.BuildSchedule(100_000m, 12m, 12, new DateTime(2025, 7, 5));

        // Assert
        Assert.Equal(106_618.56m, tabla.Sum(e => e.Value));
    }

    [Fact]
    public void TotalToPay_EjemploDelSpec_Devuelve106618_56()
    {
        // Act
        var total = AmortizationCalculator.TotalToPay(100_000m, 12m, 12);

        // Assert
        Assert.Equal(106_618.56m, total);
    }

    [Fact]
    public void CalculateMonthlyInstallment_TasaCero_DevuelvePrincipalEntrePlazo()
    {
        // Arrange
        decimal principal = 60_000m;
        int plazo = 12;

        // Act
        var cuota = AmortizationCalculator.CalculateMonthlyInstallment(principal, 0m, plazo);

        // Assert: C = P / n
        Assert.Equal(5_000m, cuota);
    }

    [Fact]
    public void BuildSchedule_PrestamoDel5DeJulio_PrimeraCuotaVenceEl5DeAgosto()
    {
        // Arrange
        var fechaCreacion = new DateTime(2025, 7, 5);

        // Act
        var tabla = AmortizationCalculator.BuildSchedule(100_000m, 12m, 12, fechaCreacion);

        // Assert: la 1ª cuota vence el MISMO DÍA del mes siguiente.
        Assert.Equal(new DateTime(2025, 8, 5), tabla[0].DueDate);
    }

    [Fact]
    public void BuildSchedule_CuotasMensualesConsecutivas_MismoDiaDeCadaMes()
    {
        // Arrange + Act
        var tabla = AmortizationCalculator.BuildSchedule(100_000m, 12m, 12, new DateTime(2025, 7, 5));

        // Assert
        for (var i = 0; i < tabla.Count; i++)
            Assert.Equal(new DateTime(2025, 7, 5).AddMonths(i + 1), tabla[i].DueDate);
    }

    [Fact]
    public void BuildSchedule_CapitalDeLaTabla_SumaExactamenteElPrincipal()
    {
        // Arrange + Act
        var tabla = AmortizationCalculator.BuildSchedule(100_000m, 12m, 12, DateTime.Today);

        // Assert
        Assert.Equal(100_000m, tabla.Sum(e => e.PrincipalAmount));
    }

    [Theory]
    [InlineData(100_000, 12, 12)]
    [InlineData(250_000, 18.5, 36)]
    [InlineData(1_234.56, 7.25, 6)]
    public void BuildSchedule_CapitalSumaElPrincipal_EnDistintosEscenarios(
        decimal principal, decimal tasa, int plazo)
    {
        // Act
        var tabla = AmortizationCalculator.BuildSchedule(principal, tasa, plazo, DateTime.Today);

        // Assert
        Assert.Equal(principal, tabla.Sum(e => e.PrincipalAmount));
    }

    [Fact]
    public void BuildSchedule_ConTasaPositiva_CuotaFijaEnTodasLasFilas()
    {
        // Arrange
        var cuotaEsperada = AmortizationCalculator.CalculateMonthlyInstallment(100_000m, 12m, 12);

        // Act
        var tabla = AmortizationCalculator.BuildSchedule(100_000m, 12m, 12, DateTime.Today);

        // Assert: la cuota es fija en TODAS las filas (incluida la última).
        Assert.All(tabla, e => Assert.Equal(cuotaEsperada, e.Value));
    }

    [Fact]
    public void BuildSchedule_TasaCero_SinInteresesYCapitalIgualALaCuota()
    {
        // Act
        var tabla = AmortizationCalculator.BuildSchedule(60_000m, 0m, 12, DateTime.Today);

        // Assert
        Assert.All(tabla, e => Assert.Equal(0m, e.InterestAmount));
        Assert.Equal(60_000m, tabla.Sum(e => e.PrincipalAmount));
        Assert.All(tabla, e => Assert.Equal(e.PrincipalAmount, e.Value));
    }

    [Fact]
    public void BuildSchedule_GeneraTantasCuotasComoMesesDelPlazo_NumeradasDesde1()
    {
        // Act
        var tabla = AmortizationCalculator.BuildSchedule(100_000m, 12m, 24, DateTime.Today);

        // Assert
        Assert.Equal(24, tabla.Count);
        Assert.Equal(Enumerable.Range(1, 24), tabla.Select(e => e.Number));
    }

    [Fact]
    public void BuildSchedule_CadaCuota_InteresMasCapitalIgualAlValor()
    {
        // Act
        var tabla = AmortizationCalculator.BuildSchedule(100_000m, 12m, 12, DateTime.Today);

        // Assert
        Assert.All(tabla, e => Assert.Equal(e.Value, e.InterestAmount + e.PrincipalAmount));
    }
}
