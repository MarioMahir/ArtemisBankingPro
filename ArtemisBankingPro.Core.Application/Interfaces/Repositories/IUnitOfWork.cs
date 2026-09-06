namespace ArtemisBankingPro.Core.Application.Interfaces.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta la operación dentro de una transacción de base de datos:
    /// obligatorio en operaciones que afectan varias entidades (transferencias,
    /// cancelación de cuentas con balance, Hermes Pay, desembolsos).
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
