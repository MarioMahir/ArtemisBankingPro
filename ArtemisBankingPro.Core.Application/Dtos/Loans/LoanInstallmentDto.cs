using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Application.Dtos.Loans;

public class LoanInstallmentDto
{
    public int Number { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Value { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public InstallmentStatus Status { get; set; }
    public bool IsOverdue { get; set; }
}
