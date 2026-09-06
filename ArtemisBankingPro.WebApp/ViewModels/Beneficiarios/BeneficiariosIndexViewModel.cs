using ArtemisBankingPro.Core.Application.Dtos.Beneficiaries;

namespace ArtemisBankingPro.WebApp.ViewModels.Beneficiarios;

public class BeneficiariosIndexViewModel
{
    public List<BeneficiaryDto> Beneficiarios { get; set; } = [];
    public AgregarBeneficiarioViewModel Nuevo { get; set; } = new();
}
