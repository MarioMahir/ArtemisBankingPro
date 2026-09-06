namespace ArtemisBankingPro.Core.Application.Dtos.Loans;

/// <summary>Cliente activo sin préstamo activo, candidato a asignación de préstamo.</summary>
public class EligibleClientDto
{
    public string UserId { get; set; } = null!;
    public string Identification { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public decimal TotalDebt { get; set; }
}
