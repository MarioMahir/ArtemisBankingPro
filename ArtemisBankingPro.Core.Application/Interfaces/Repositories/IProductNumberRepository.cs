namespace ArtemisBankingPro.Core.Application.Interfaces.Repositories;

/// <summary>
/// Verificación de unicidad de números de producto. Los números de cuenta de ahorro
/// y de préstamo comparten un mismo espacio de 9 dígitos: un número nuevo no puede
/// existir en NINGUNA de las dos tablas.
/// </summary>
public interface IProductNumberRepository
{
    Task<bool> AccountOrLoanNumberExistsAsync(string number);
    Task<bool> CardNumberExistsAsync(string number);
}
