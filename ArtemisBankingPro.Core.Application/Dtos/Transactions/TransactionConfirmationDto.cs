namespace ArtemisBankingPro.Core.Application.Dtos.Transactions;

/// <summary>
/// Datos para las pantallas de confirmación previa exigidas por el spec:
/// titulares, números (enmascarados cuando aplica) y montos (ingresado y efectivo).
/// </summary>
public class TransactionConfirmationDto
{
    public string? SourceAccountNumber { get; set; }
    public string? SourceHolderFullName { get; set; }

    public string? DestinationAccountNumber { get; set; }
    public string? DestinationHolderFullName { get; set; }

    /// <summary>Últimos 4 de la tarjeta o número de préstamo, según la operación.</summary>
    public string? ProductReference { get; set; }

    public decimal RequestedAmount { get; set; }

    /// <summary>Monto que realmente se debitará (regla anti-sobrepago).</summary>
    public decimal EffectiveAmount { get; set; }
}
