using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }

    public int AccountId { get; set; }
    public SavingsAccount Account { get; set; } = null!;

    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }

    /// <summary>Destino: nº de cuenta, "RETIRO", últimos 4 de tarjeta o nº de préstamo (ver docs/reglas-negocio.md §9).</summary>
    public string Beneficiary { get; set; } = null!;

    /// <summary>Origen: nº de cuenta, "DEPÓSITO", últimos 4 de tarjeta o nº de préstamo.</summary>
    public string Origin { get; set; } = null!;

    public TransactionStatus Status { get; set; }

    /// <summary>Cajero responsable cuando la operación se realizó en ventanilla.</summary>
    public string? CashierId { get; set; }

    /// <summary>Marca los pagos a tarjeta/préstamo para los indicadores de "pagos".</summary>
    public bool IsPayment { get; set; }

    public DateTime CreatedAt { get; set; }
}
