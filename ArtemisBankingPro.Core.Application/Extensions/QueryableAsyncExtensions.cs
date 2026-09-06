using System.Linq.Expressions;

namespace ArtemisBankingPro.Core.Application.Extensions;

/// <summary>
/// Operadores async sobre <see cref="IQueryable{T}"/> implementados únicamente con
/// <see cref="IAsyncEnumerable{T}"/> (BCL), para que Core.Application no dependa de
/// Entity Framework Core y la arquitectura Onion quede sin fugas de infraestructura.
/// Con proveedores que traducen la consulta (EF Core en Persistence, MockQueryable en
/// tests) la ejecución es asíncrona y ocurre en el servidor; con proveedores en memoria
/// se ejecuta de forma síncrona como fallback.
/// </summary>
public static class QueryableAsyncExtensions
{
    public static async Task<List<T>> ToListAsync<T>(
        this IQueryable<T> source, CancellationToken cancellationToken = default)
    {
        if (source is not IAsyncEnumerable<T> asyncSource)
            return source.ToList();

        var list = new List<T>();
        await foreach (var item in asyncSource.WithCancellation(cancellationToken))
            list.Add(item);
        return list;
    }

    public static async Task<T?> FirstOrDefaultAsync<T>(
        this IQueryable<T> source, CancellationToken cancellationToken = default)
    {
        var first = source.Take(1);
        if (first is not IAsyncEnumerable<T> asyncSource)
            return first.FirstOrDefault();

        await foreach (var item in asyncSource.WithCancellation(cancellationToken))
            return item;
        return default;
    }

    public static Task<T?> FirstOrDefaultAsync<T>(
        this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => source.Where(predicate).FirstOrDefaultAsync(cancellationToken);

    public static async Task<bool> AnyAsync<T>(
        this IQueryable<T> source, CancellationToken cancellationToken = default)
    {
        var first = source.Select(_ => 1).Take(1);
        if (first is not IAsyncEnumerable<int> asyncSource)
            return first.Any();

        await foreach (var _ in asyncSource.WithCancellation(cancellationToken))
            return true;
        return false;
    }

    public static Task<bool> AnyAsync<T>(
        this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => source.Where(predicate).AnyAsync(cancellationToken);

    public static async Task<int> CountAsync<T>(
        this IQueryable<T> source, CancellationToken cancellationToken = default)
    {
        // El GroupBy por constante hace que el proveedor traduzca a un COUNT en el servidor.
        var count = source.GroupBy(_ => 1).Select(g => g.Count());
        if (count is not IAsyncEnumerable<int> asyncSource)
            return source.Count();

        await foreach (var value in asyncSource.WithCancellation(cancellationToken))
            return value;
        return 0;
    }

    public static Task<int> CountAsync<T>(
        this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => source.Where(predicate).CountAsync(cancellationToken);

    public static async Task<decimal> SumAsync<T>(
        this IQueryable<T> source, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        // El GroupBy por constante hace que el proveedor traduzca a un SUM en el servidor.
        var sum = source.Select(selector).GroupBy(_ => 1).Select(g => g.Sum());
        if (sum is not IAsyncEnumerable<decimal> asyncSource)
            return source.Select(selector).Sum();

        await foreach (var value in asyncSource.WithCancellation(cancellationToken))
            return value;
        return 0m;
    }
}
