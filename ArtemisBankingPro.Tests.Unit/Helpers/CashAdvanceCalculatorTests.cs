using ArtemisBankingPro.Core.Application.Helpers;

namespace ArtemisBankingPro.Tests.Unit.Helpers;

public class CashAdvanceCalculatorTests
{
    // Ejemplo EXACTO del spec: límite 500, deuda 300 → disponible 200.
    [Fact]
    public void AvailableCredit_Limite500Deuda300_Devuelve200()
    {
        Assert.Equal(200m, CashAdvanceCalculator.AvailableCredit(500m, 300m));
    }

    [Fact]
    public void TotalToCharge_Avance200_Devuelve212_50()
    {
        // 200 + 6.25% = 212.50 (ejemplo del spec).
        Assert.Equal(212.50m, CashAdvanceCalculator.TotalToCharge(200m));
    }

    [Fact]
    public void IsApproved_Avance200ConDisponible200_EsRechazado()
    {
        // Ejemplo del spec: total 212.50 > disponible 200 → RECHAZADO.
        Assert.False(CashAdvanceCalculator.IsApproved(200m, 500m, 300m));
    }

    [Fact]
    public void Interest_Avance100_Devuelve6_25()
    {
        // Ejemplo del spec.
        Assert.Equal(6.25m, CashAdvanceCalculator.Interest(100m));
    }

    [Fact]
    public void TotalToCharge_Avance100_Devuelve106_25()
    {
        Assert.Equal(106.25m, CashAdvanceCalculator.TotalToCharge(100m));
    }

    [Fact]
    public void IsApproved_Avance100ConDisponible200_EsAprobado()
    {
        // 106.25 ≤ 200 → aprobado (ejemplo del spec).
        Assert.True(CashAdvanceCalculator.IsApproved(100m, 500m, 300m));
    }

    [Fact]
    public void IsApproved_TotalExactamenteIgualAlDisponible_EsAprobado()
    {
        // total = 106.25; disponible = 106.25 → la regla es "≤".
        Assert.True(CashAdvanceCalculator.IsApproved(100m, 406.25m, 300m));
    }

    [Fact]
    public void Interest_SeRedondeaADosDecimales()
    {
        // 33.33 * 0.0625 = 2.083125 → 2.08
        Assert.Equal(2.08m, CashAdvanceCalculator.Interest(33.33m));
    }
}
