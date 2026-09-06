using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class Loan
{
    public int Id { get; set; }

    /// <summary>9 dígitos, texto. Espacio de numeración compartido con cuentas de ahorro.</summary>
    public string LoanNumber { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public string AdminUserId { get; set; } = null!;

    public decimal ApprovedAmount { get; set; }

    /// <summary>Plazo en meses: 6–60 en múltiplos de 6.</summary>
    public int TermMonths { get; set; }

    public decimal AnnualInterestRate { get; set; }
    public LoanStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<LoanInstallment> Installments { get; set; } = [];
}
