namespace ArtemisBankingPro.Core.Application.Dtos.Users;

public class UpdateUserRequest
{
    public string Id { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Identification { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;

    /// <summary>Opcional: si viene vacía, la contraseña actual no se modifica.</summary>
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }

    /// <summary>Se suma al balance de la cuenta principal (Cliente/Comercio) registrando un CRÉDITO.</summary>
    public decimal? AdditionalAmount { get; set; }
}
