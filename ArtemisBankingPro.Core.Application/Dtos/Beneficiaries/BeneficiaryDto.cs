namespace ArtemisBankingPro.Core.Application.Dtos.Beneficiaries;

public class BeneficiaryDto
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = null!;
    public string HolderFirstName { get; set; } = null!;
    public string HolderLastName { get; set; } = null!;
    public string HolderFullName => $"{HolderFirstName} {HolderLastName}";
    public DateTime CreatedAt { get; set; }
}
