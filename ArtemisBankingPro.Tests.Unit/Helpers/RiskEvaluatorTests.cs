using ArtemisBankingPro.Core.Application.Helpers;

namespace ArtemisBankingPro.Tests.Unit.Helpers;

public class RiskEvaluatorTests
{
    [Fact]
    public void Evaluate_DeudaActualSuperaElPromedio_DevuelveCurrentHighRisk()
    {
        // Arrange: deuda actual 60,000 > promedio 50,000
        // Act
        var riesgo = RiskEvaluator.Evaluate(60_000m, 10_000m, 50_000m);

        // Assert
        Assert.Equal(RiskLevel.CurrentHighRisk, riesgo);
    }

    [Fact]
    public void Evaluate_DeudaProyectadaSuperaElPromedio_DevuelveProjectedHighRisk()
    {
        // Arrange: actual 30,000 ≤ 50,000, pero 30,000 + 25,000 = 55,000 > 50,000
        // Act
        var riesgo = RiskEvaluator.Evaluate(30_000m, 25_000m, 50_000m);

        // Assert
        Assert.Equal(RiskLevel.ProjectedHighRisk, riesgo);
    }

    [Fact]
    public void Evaluate_AmbasDeudasBajoElPromedio_DevuelveNone()
    {
        // Act
        var riesgo = RiskEvaluator.Evaluate(10_000m, 20_000m, 50_000m);

        // Assert
        Assert.Equal(RiskLevel.None, riesgo);
    }

    [Fact]
    public void Evaluate_DeudaActualIgualAlPromedio_NoEsAltoRiesgoActual()
    {
        // Arrange: igual al promedio NO supera el promedio (regla es estrictamente mayor).
        // Act
        var riesgo = RiskEvaluator.Evaluate(50_000m, 0m, 50_000m);

        // Assert
        Assert.Equal(RiskLevel.None, riesgo);
    }

    [Fact]
    public void Evaluate_DeudaProyectadaIgualAlPromedio_NoEsAltoRiesgo()
    {
        // Arrange: 20,000 + 30,000 = 50,000 == promedio → no supera.
        // Act
        var riesgo = RiskEvaluator.Evaluate(20_000m, 30_000m, 50_000m);

        // Assert
        Assert.Equal(RiskLevel.None, riesgo);
    }

    [Fact]
    public void Evaluate_PromedioCeroYClienteSinDeuda_ConNuevoPrestamo_EsProyectadoAltoRiesgo()
    {
        // Arrange: sin clientes activos el promedio es RD$0.00; cualquier préstamo > 0 lo supera.
        // Act
        var riesgo = RiskEvaluator.Evaluate(0m, 1m, 0m);

        // Assert
        Assert.Equal(RiskLevel.ProjectedHighRisk, riesgo);
    }

    [Fact]
    public void Evaluate_DeudaActualSuperaPromedio_PrevaleceSobreElProyectado()
    {
        // Arrange: ambas condiciones ciertas → se reporta el riesgo ACTUAL.
        // Act
        var riesgo = RiskEvaluator.Evaluate(80_000m, 40_000m, 50_000m);

        // Assert
        Assert.Equal(RiskLevel.CurrentHighRisk, riesgo);
    }
}
