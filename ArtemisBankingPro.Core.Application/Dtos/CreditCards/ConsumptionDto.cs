using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Application.Dtos.CreditCards;

public class ConsumptionDto
{
    public int Id { get; set; }
    public string CardLastFourDigits { get; set; } = null!;
    public string CommerceName { get; set; } = null!;
    public decimal Amount { get; set; }
    public ConsumptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
