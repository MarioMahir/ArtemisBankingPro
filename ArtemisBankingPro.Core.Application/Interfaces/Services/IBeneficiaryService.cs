using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Beneficiaries;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

public interface IBeneficiaryService
{
    /// <summary>Beneficiarios del cliente con nombre y apellido del titular de cada cuenta.</summary>
    Task<List<BeneficiaryDto>> GetByUserAsync(string userId);

    /// <summary>Agrega por número de cuenta: 9 dígitos, existente, activa, no propia, no duplicada.</summary>
    Task<ServiceResult> AddAsync(string userId, string accountNumber);

    /// <summary>Elimina solo la relación (la cuenta no se toca).</summary>
    Task<ServiceResult> RemoveAsync(string userId, int beneficiaryId);
}
