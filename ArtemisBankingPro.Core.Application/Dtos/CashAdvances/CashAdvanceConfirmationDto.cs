namespace ArtemisBankingPro.Core.Application.Dtos.CashAdvances;

/// <summary>Datos de confirmación previa de un avance de efectivo.</summary>
public class CashAdvanceConfirmationDto
{
    public string CardLastFourDigits { get; set; } = null!;
    public string DestinationAccountNumber { get; set; } = null!;
    public decimal AdvanceAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal TotalToCharge { get; set; }
    public decimal AvailableCredit { get; set; }
}
