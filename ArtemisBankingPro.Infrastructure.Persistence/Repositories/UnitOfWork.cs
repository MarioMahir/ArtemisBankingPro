using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        // SQLite in-memory (tests) no soporta transacciones anidadas de la misma manera;
        // si ya hay una transacción activa, se reutiliza.
        if (context.Database.CurrentTransaction is not null)
        {
            await operation();
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await operation();
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
