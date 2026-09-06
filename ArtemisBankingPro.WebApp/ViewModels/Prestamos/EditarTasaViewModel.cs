using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Prestamos;

public class EditarTasaViewModel
{
    [Required]
    public int LoanId { get; set; }

    public string? LoanNumber { get; set; }
    public string? ClienteNombre { get; set; }
    public decimal TasaActual { get; set; }

    [Required(ErrorMessage = Mensajes.TasaNegativa)]
    [Range(0, 100, ErrorMessage = Mensajes.TasaNegativa)]
    [Display(Name = "Nueva tasa de interés anual (%)")]
    public decimal? NuevaTasa { get; set; }
}
