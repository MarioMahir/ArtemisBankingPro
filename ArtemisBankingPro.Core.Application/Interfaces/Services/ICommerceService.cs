using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Commerces;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

public interface ICommerceService : IGenericService<CommerceDto>
{
    /// <summary>Listado paginado; default solo activos (isActive null → activos).</summary>
    Task<PagedResult<CommerceDto>> GetPagedAsync(int page, bool? isActive);

    /// <summary>RNC y correo únicos. El comercio nace Activo y SIN usuario.</summary>
    Task<ServiceResult<CommerceDto>> CreateAsync(SaveCommerceRequest request);

    Task<ServiceResult> UpdateAsync(int id, SaveCommerceRequest request);

    /// <summary>Desactivar inactiva sus usuarios; reactivar NO los reactiva.</summary>
    Task<ServiceResult> SetStatusAsync(int id, bool isActive);
}
