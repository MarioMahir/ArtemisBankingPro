namespace ArtemisBankingPro.Core.Domain.Entities;

public class Beneficiary
{
    public int Id { get; set; }

    /// <summary>Cliente dueño de la lista de beneficiarios.</summary>
    public string UserId { get; set; } = null!;

    /// <summary>Cuenta ajena registrada como beneficiaria.</summary>
    public int SavingsAccountId { get; set; }
    public SavingsAccount SavingsAccount { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
