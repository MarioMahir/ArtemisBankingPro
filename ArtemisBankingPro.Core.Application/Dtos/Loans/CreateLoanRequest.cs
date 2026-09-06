namespace ArtemisBankingPro.Core.Application.Dtos.Loans;

public class CreateLoanRequest
{
    public string ClientId { get; set; } = null!;
    public decimal CapitalAmount { get; set; }
    public int TermInMonths { get; set; }
    public decimal AnnualInterestRate { get; set; }
}
