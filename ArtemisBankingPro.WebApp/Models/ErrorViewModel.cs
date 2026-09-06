namespace ArtemisBankingPro.WebApp.Models;

/// <summary>Modelo de la página de error amigable (estilo Problem Details RFC 7807).</summary>
public class ErrorViewModel
{
    public int Status { get; set; } = 500;
    public string Title { get; set; } = "Ha ocurrido un error inesperado";
    public string Detail { get; set; } = "Lo sentimos, no fue posible completar la operación. Intente nuevamente más tarde.";
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
