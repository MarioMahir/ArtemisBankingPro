using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;

namespace ArtemisBankingPro.WebApp.ViewModels.Cuentas;

public class CuentasIndexViewModel
{
    public PagedResult<SavingsAccountDto> Cuentas { get; set; } = new();

    /// <summary>Filtro de estado: Activas (default) / Canceladas / Todas.</summary>
    public string Estado { get; set; } = "Activas";

    /// <summary>Filtro de tipo: Todas (default) / Principal / Secundaria.</summary>
    public string Tipo { get; set; } = "Todas";

    public string? Cedula { get; set; }
}
