namespace ArtemisBankingPro.WebApp.ViewModels.Prestamos;

/// <summary>Pantalla de advertencia de alto riesgo previa a la asignación del préstamo.</summary>
public class ConfirmarRiesgoViewModel
{
    public string ClientId { get; set; } = null!;
    public string? ClienteNombre { get; set; }
    public int TermInMonths { get; set; }
    public decimal CapitalAmount { get; set; }
    public decimal AnnualInterestRate { get; set; }

    /// <summary>Mensaje exacto del spec según el tipo de riesgo.</summary>
    public string Advertencia { get; set; } = null!;

    public decimal DeudaActual { get; set; }
    public decimal DeudaProyectada { get; set; }
    public decimal DeudaPromedio { get; set; }
    public decimal TotalAPagarNuevoPrestamo { get; set; }
}
