using ArtemisBankingPro.Core.Application.Common;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

/// <summary>
/// Operaciones de lectura comunes a los servicios de negocio cuyo agregado
/// se identifica por clave entera y se expone como DTO.
/// </summary>
public interface IGenericService<TDto>
{
    Task<List<TDto>> GetAllAsync();
    Task<ServiceResult<TDto>> GetByIdAsync(int id);
}
