namespace ArtemisBankingPro.Core.Application.Helpers;

public enum RiskLevel
{
    /// <summary>La deuda no supera el promedio: se puede asignar sin advertencia.</summary>
    None,

    /// <summary>La deuda ACTUAL del cliente ya supera la deuda promedio del sistema.</summary>
    CurrentHighRisk,

    /// <summary>La deuda actual no supera el promedio, pero la proyectada (actual + total a pagar del nuevo préstamo) sí.</summary>
    ProjectedHighRisk
}

public static class RiskEvaluator
{
    public static RiskLevel Evaluate(decimal currentDebt, decimal newLoanTotalToPay, decimal averageDebt)
    {
        if (currentDebt > averageDebt) return RiskLevel.CurrentHighRisk;
        if (currentDebt + newLoanTotalToPay > averageDebt) return RiskLevel.ProjectedHighRisk;
        return RiskLevel.None;
    }
}
