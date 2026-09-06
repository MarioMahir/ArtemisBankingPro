using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Prestamos;

/// <summary>Paso 2 de asignación de préstamo: plazo, monto y tasa.</summary>
public class DatosPrestamoViewModel
{
    [Required(ErrorMessage = Mensajes.DebeSeleccionarCliente)]
    public string ClientId { get; set; } = null!;

    public string? ClienteNombre { get; set; }
    public string? ClienteCedula { get; set; }

    [Required(ErrorMessage = Mensajes.PlazoInvalido)]
    [Display(Name = "Plazo (meses)")]
    public int? TermInMonths { get; set; }

    [Required(ErrorMessage = Mensajes.MontoPrestamoInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.MontoPrestamoInvalido)]
    [Display(Name = "Monto a prestar")]
    public decimal? CapitalAmount { get; set; }

    [Required(ErrorMessage = Mensajes.TasaNegativa)]
    [Range(0, 100, ErrorMessage = Mensajes.TasaNegativa)]
    [Display(Name = "Tasa de interés anual (%)")]
    public decimal? AnnualInterestRate { get; set; }
}
