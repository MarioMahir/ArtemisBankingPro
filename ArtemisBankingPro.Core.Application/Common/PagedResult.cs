using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.Core.Application.Common;

/// <summary>Resultado paginado. El tamaño de página del sistema es fijo: 20.</summary>
public class PagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; } = AppConstants.PageSize;
    public int TotalCount { get; init; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    /// <summary>Normaliza page/pageSize según la regla del spec (máximo 20 por página).</summary>
    public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
    {
        var p = page.GetValueOrDefault(1) < 1 ? 1 : page.GetValueOrDefault(1);
        var size = pageSize.GetValueOrDefault(AppConstants.PageSize);
        if (size < 1 || size > AppConstants.PageSize) size = AppConstants.PageSize;
        return (p, size);
    }
}
