using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Prestamos;

/// <summary>Paso 1 de asignación de préstamo: selección de cliente elegible por radio button.</summary>
public class SeleccionarClienteViewModel
{
    public List<EligibleClientDto> Clientes { get; set; } = [];

    /// <summary>Deuda promedio de los clientes activos del sistema (encabezado).</summary>
    public decimal DeudaPromedio { get; set; }

    [Required(ErrorMessage = Mensajes.DebeSeleccionarCliente)]
    public string? ClientId { get; set; }
}
