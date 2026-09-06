using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Tarjetas;

public class EditarLimiteViewModel
{
    [Required]
    public int CardId { get; set; }

    public string? UltimosCuatro { get; set; }
    public string? ClienteNombre { get; set; }
    public decimal LimiteActual { get; set; }
    public decimal Deuda { get; set; }

    [Required(ErrorMessage = Mensajes.LimiteTarjetaInvalido)]
    [Range(0.01, 999999999999.99, ErrorMessage = Mensajes.LimiteTarjetaInvalido)]
    [Display(Name = "Nuevo límite de crédito")]
    public decimal? NuevoLimite { get; set; }
}
