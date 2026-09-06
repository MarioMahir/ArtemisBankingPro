using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Application.Dtos.CreditCards;

/// <summary>
/// JAMÁS expone el número completo de la tarjeta ni el hash del CVC:
/// solo el número enmascarado y los últimos 4 dígitos.
/// </summary>
public class CreditCardDto
{
    public int Id { get; set; }
    public string MaskedCardNumber { get; set; } = null!;
    public string LastFourDigits { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string? ClientFullName { get; set; }
    public string? ClientIdentification { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal OwedAmount { get; set; }
    public decimal AvailableCredit => CreditLimit - OwedAmount;
    public DateTime ExpirationDate { get; set; }

    /// <summary>Expiración en formato MM/AA.</summary>
    public string ExpirationDisplay { get; set; } = null!;

    public ProductStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Consumos (solo en el detalle), más recientes primero.</summary>
    public List<ConsumptionDto> Consumptions { get; set; } = [];
}
