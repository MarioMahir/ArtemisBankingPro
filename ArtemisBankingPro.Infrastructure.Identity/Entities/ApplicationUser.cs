using Microsoft.AspNetCore.Identity;

namespace ArtemisBankingPro.Infrastructure.Identity.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;

    /// <summary>Cédula: texto (conserva ceros iniciales), única en el sistema.</summary>
    public string Identification { get; set; } = null!;

    public bool IsActive { get; set; }

    /// <summary>Comercio asociado (solo usuarios de rol Comercio; máximo 1 usuario por comercio).</summary>
    public int? CommerceId { get; set; }

    public DateTime CreatedAt { get; set; }
}
