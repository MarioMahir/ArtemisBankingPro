namespace ArtemisBankingPro.Core.Application.Dtos.Commerces;

/// <summary>Datos de creación/edición de un comercio.</summary>
public class SaveCommerceRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Rnc { get; set; } = null!;
}
