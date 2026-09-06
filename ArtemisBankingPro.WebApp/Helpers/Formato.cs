using System.Globalization;

namespace ArtemisBankingPro.WebApp.Helpers;

/// <summary>Formatos de presentación del spec: RD$#,##0.00, dd/MM/yyyy y MM/AA.</summary>
public static class Formato
{
    public static string Monto(decimal valor) =>
        "RD$" + valor.ToString("#,##0.00", CultureInfo.InvariantCulture);

    public static string Fecha(DateTime fecha) => fecha.ToString("dd/MM/yyyy");

    public static string FechaHora(DateTime fecha) => fecha.ToString("dd/MM/yyyy hh:mm tt");

    public static string Expiracion(DateTime fecha) => fecha.ToString("MM/yy");
}
