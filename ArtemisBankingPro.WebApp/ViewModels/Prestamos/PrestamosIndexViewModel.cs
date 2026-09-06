using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Loans;

namespace ArtemisBankingPro.WebApp.ViewModels.Prestamos;

public class PrestamosIndexViewModel
{
    public PagedResult<LoanDto> Prestamos { get; set; } = new();

    /// <summary>Filtro de estado: Activos (default) / Completados / Todos.</summary>
    public string Estado { get; set; } = "Activos";

    /// <summary>Búsqueda por cédula del cliente.</summary>
    public string? Cedula { get; set; }
}
