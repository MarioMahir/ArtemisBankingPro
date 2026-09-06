namespace ArtemisBankingPro.Core.Application.Dtos.Users;

public class CreateUserRequest
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Identification { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
    public string Role { get; set; } = null!;

    /// <summary>Solo aplica a Cliente (opcional) y a usuarios Comercio (requerido en la API).</summary>
    public decimal? InitialAmount { get; set; }

    /// <summary>Solo para usuarios de tipo Comercio.</summary>
    public int? CommerceId { get; set; }

    /// <summary>true → el correo lleva el token en el cuerpo (API); false → enlace de activación (WebApp).</summary>
    public bool TokenInBody { get; set; }
}
