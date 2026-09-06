using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Application.Dtos.Loans;

public class LoanDto
{
    public int Id { get; set; }
    public string LoanNumber { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string? ClientFullName { get; set; }
    public string? ClientIdentification { get; set; }
    public decimal ApprovedAmount { get; set; }
    public int TermMonths { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public decimal PendingAmount { get; set; }

    /// <summary>Cuota mensual fija (sistema francés). Campo exigido por el ejemplo del spec en la API.</summary>
    public decimal MonthlyInstallment { get; set; }

    /// <summary>Total a pagar = suma de todas las cuotas de la amortización.</summary>
    public decimal TotalAmountToPay { get; set; }
    public LoanStatus Status { get; set; }

    /// <summary>true = en mora (al menos una cuota atrasada); false = al día.</summary>
    public bool IsInArrears { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Tabla de amortización (solo en el detalle). El spec la nombra "amortization" en la API.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("amortization")]
    public List<LoanInstallmentDto> Installments { get; set; } = [];
}
