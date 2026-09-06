using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;

public class SavingsAccountDto
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = null!;
    public decimal Balance { get; set; }
    public AccountType Type { get; set; }
    public ProductStatus Status { get; set; }
    public string UserId { get; set; } = null!;
    public string? ClientFullName { get; set; }
    public string? ClientIdentification { get; set; }
    public DateTime CreatedAt { get; set; }
}
