using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Beneficiarios;

public class AgregarBeneficiarioViewModel
{
    [Required(ErrorMessage = "El número de cuenta es obligatorio.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = Mensajes.CuentaInvalida)]
    [Display(Name = "Número de cuenta")]
    public string? AccountNumber { get; set; }
}
