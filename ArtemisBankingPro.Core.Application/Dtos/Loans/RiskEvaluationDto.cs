using ArtemisBankingPro.Core.Application.Helpers;

namespace ArtemisBankingPro.Core.Application.Dtos.Loans;

public class RiskEvaluationDto
{
    public RiskLevel RiskLevel { get; set; }

    /// <summary>Mensaje de advertencia exacto del spec cuando hay alto riesgo; null si no lo hay.</summary>
    public string? WarningMessage { get; set; }

    public decimal CurrentDebt { get; set; }
    public decimal ProjectedDebt { get; set; }
    public decimal AverageDebt { get; set; }

    /// <summary>Total a pagar del nuevo préstamo (suma de todas las cuotas).</summary>
    public decimal NewLoanTotalToPay { get; set; }
}
