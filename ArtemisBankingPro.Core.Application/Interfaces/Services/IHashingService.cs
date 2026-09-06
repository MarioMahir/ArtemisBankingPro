namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

public interface IHashingService
{
    /// <summary>Hash SHA-256 en hexadecimal. Usado para el CVC (nunca se guarda en claro).</summary>
    string Sha256(string value);
}
