namespace ArtemisBankingPro.Infrastructure.Identity.Entities;

public enum VerificationTokenType
{
    Activation = 1,
    PasswordReset = 2
}

/// <summary>
/// Tokens de activación y de restablecimiento: asociados a un usuario, de un solo
/// uso. Los de restablecimiento expiran a los 30 minutos de generados.
/// </summary>
public class VerificationToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string Token { get; set; } = null!;
    public VerificationTokenType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsUsed { get; set; }
}
