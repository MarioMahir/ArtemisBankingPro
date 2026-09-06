using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class SavingsAccount
{
    public int Id { get; set; }

    /// <summary>9 dígitos, texto para conservar ceros iniciales. Espacio de numeración compartido con préstamos.</summary>
    public string AccountNumber { get; set; } = null!;

    public decimal Balance { get; set; }
    public AccountType Type { get; set; }
    public ProductStatus Status { get; set; }

    /// <summary>Id del cliente (o usuario Comercio) propietario.</summary>
    public string UserId { get; set; } = null!;

    /// <summary>Administrador que asignó la cuenta (null en la principal creada por el sistema).</summary>
    public string? AdminUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = [];
}
