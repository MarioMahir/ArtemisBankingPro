using ArtemisBankingPro.Core.Application.Common;

namespace ArtemisBankingPro.WebApp.ViewModels.Shared;

/// <summary>Modelo del partial reutilizable de paginación (_Paginacion).</summary>
public class PaginacionViewModel
{
    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public string Accion { get; set; } = "Index";

    /// <summary>Valores de ruta adicionales (filtros) que se conservan al cambiar de página.</summary>
    public Dictionary<string, string> Ruta { get; set; } = [];

    public static PaginacionViewModel De<T>(PagedResult<T> paged, string accion = "Index", Dictionary<string, string>? ruta = null) =>
        new()
        {
            Pagina = paged.Page,
            TotalPaginas = paged.TotalPages,
            Accion = accion,
            Ruta = ruta ?? []
        };
}
