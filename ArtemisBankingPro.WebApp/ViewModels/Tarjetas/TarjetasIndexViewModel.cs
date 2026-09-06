using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;

namespace ArtemisBankingPro.WebApp.ViewModels.Tarjetas;

public class TarjetasIndexViewModel
{
    public PagedResult<CreditCardDto> Tarjetas { get; set; } = new();

    /// <summary>Filtro de estado: Activas (default) / Canceladas / Todas.</summary>
    public string Estado { get; set; } = "Activas";

    public string? Cedula { get; set; }
}
