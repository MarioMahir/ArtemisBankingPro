using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class CardConsumption
{
    public int Id { get; set; }

    public int CreditCardId { get; set; }
    public CreditCard CreditCard { get; set; } = null!;

    /// <summary>Comercio de Hermes Pay; null cuando el consumo es un avance de efectivo.</summary>
    public int? CommerceId { get; set; }
    public Commerce? Commerce { get; set; }

    /// <summary>Nombre a mostrar: nombre del comercio o "AVANCE".</summary>
    public string CommerceName { get; set; } = null!;

    /// <summary>En avances incluye el interés (avance + 6.25%).</summary>
    public decimal Amount { get; set; }

    public ConsumptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
