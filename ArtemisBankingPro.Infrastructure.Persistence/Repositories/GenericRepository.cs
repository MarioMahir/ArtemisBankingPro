using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class GenericRepository<TEntity>(AppDbContext context) : IGenericRepository<TEntity>
    where TEntity : class
{
    protected readonly AppDbContext Context = context;

    public Task<List<TEntity>> GetAllAsync() =>
        Context.Set<TEntity>().AsNoTracking().ToListAsync();

    public async Task<TEntity?> GetByIdAsync(params object[] keyValues) =>
        await Context.Set<TEntity>().FindAsync(keyValues);

    public IQueryable<TEntity> Query() =>
        Context.Set<TEntity>().AsNoTracking();

    public async Task AddAsync(TEntity entity) =>
        await Context.Set<TEntity>().AddAsync(entity);

    public void Update(TEntity entity) =>
        Context.Set<TEntity>().Update(entity);

    public void Delete(TEntity entity) =>
        Context.Set<TEntity>().Remove(entity);
}
