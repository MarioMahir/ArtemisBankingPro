namespace ArtemisBankingPro.Core.Application.Interfaces.Repositories;

public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<List<TEntity>> GetAllAsync();
    Task<TEntity?> GetByIdAsync(params object[] keyValues);

    /// <summary>IQueryable sin tracking para composición de consultas en los servicios.</summary>
    IQueryable<TEntity> Query();

    Task AddAsync(TEntity entity);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}
