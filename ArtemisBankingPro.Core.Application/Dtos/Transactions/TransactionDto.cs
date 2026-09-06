using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Application.Dtos.Transactions;

public class TransactionDto
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = null!;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Beneficiary { get; set; } = null!;
    public string Origin { get; set; } = null!;
    public TransactionStatus Status { get; set; }
    public string? CashierId { get; set; }
    public bool IsPayment { get; set; }
    public DateTime CreatedAt { get; set; }
}
