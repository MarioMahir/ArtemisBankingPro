using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class LoanInstallment
{
    public int Id { get; set; }

    public int LoanId { get; set; }
    public Loan Loan { get; set; } = null!;

    public int Number { get; set; }
    public DateTime DueDate { get; set; }

    /// <summary>Valor total de la cuota (capital + interés).</summary>
    public decimal Value { get; set; }

    public decimal InterestAmount { get; set; }
    public decimal PrincipalAmount { get; set; }

    /// <summary>Lo que falta por pagar de ESTA cuota (para pagos parciales).</summary>
    public decimal PendingAmount { get; set; }

    public InstallmentStatus Status { get; set; }

    /// <summary>Marcada por el proceso diario si venció sin pagarse por completo.</summary>
    public bool IsOverdue { get; set; }
}
