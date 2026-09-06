namespace ArtemisBankingPro.Core.Domain.Entities;

public class Commerce
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;

    /// <summary>RNC único entre comercios.</summary>
    public string Rnc { get; set; } = null!;

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
