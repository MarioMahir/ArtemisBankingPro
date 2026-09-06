using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class CreditCard
{
    public int Id { get; set; }

    /// <summary>16 dígitos, texto. En UI/correos/logs SIEMPRE enmascarado (últimos 4).</summary>
    public string CardNumber { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public string AdminUserId { get; set; } = null!;

    public decimal CreditLimit { get; set; }

    /// <summary>Deuda actual. Inicia en RD$0.00 — el límite no es deuda.</summary>
    public decimal OwedAmount { get; set; }

    /// <summary>Vigencia: fecha de asignación + 3 años. Se muestra como MM/AA.</summary>
    public DateTime ExpirationDate { get; set; }

    /// <summary>Hash SHA-256 del CVC. El CVC en claro jamás se almacena ni se retorna.</summary>
    public string CvcHash { get; set; } = null!;

    public ProductStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<CardConsumption> Consumptions { get; set; } = [];
}
