using System.Linq.Expressions;
using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using AutoMapper;
using ArtemisBankingPro.Core.Application.Extensions;

namespace ArtemisBankingPro.Core.Application.Services;

/// <summary>
/// Base de los servicios de negocio: lecturas comunes (todo/por id) y el patrón
/// compartido de paginación (20 por página, más recientes primero). El mapeo
/// Entidad → DTO vive en <c>EntityMappingProfile</c>.
/// </summary>
public abstract class GenericService<TEntity, TDto>(
    IGenericRepository<TEntity> repository,
    IMapper mapper) : IGenericService<TDto>
    where TEntity : class
{
    protected IGenericRepository<TEntity> Repository { get; } = repository;
    protected IMapper Mapper { get; } = mapper;

    /// <summary>Mensaje de negocio cuando el id no existe.</summary>
    protected abstract string NotFoundMessage { get; }

    public virtual async Task<List<TDto>> GetAllAsync()
    {
        var entities = await Repository.GetAllAsync();
        return entities.Select(Mapper.Map<TDto>).ToList();
    }

    public virtual async Task<ServiceResult<TDto>> GetByIdAsync(int id)
    {
        var entity = await Repository.GetByIdAsync(id);
        return entity is null
            ? ServiceResult<TDto>.Fail(NotFoundMessage)
            : ServiceResult<TDto>.Ok(Mapper.Map<TDto>(entity));
    }

    protected async Task<PagedResult<TDto>> ToPagedAsync(
        IQueryable<TEntity> query, int page, Expression<Func<TEntity, DateTime>> orderByDescending)
    {
        var (p, size) = PagedResult<TDto>.Normalize(page, null);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(orderByDescending)
            .Skip((p - 1) * size)
            .Take(size)
            .ToListAsync();

        return new PagedResult<TDto>
        {
            Items = items.Select(Mapper.Map<TDto>).ToList(),
            Page = p,
            PageSize = size,
            TotalCount = total
        };
    }
}
