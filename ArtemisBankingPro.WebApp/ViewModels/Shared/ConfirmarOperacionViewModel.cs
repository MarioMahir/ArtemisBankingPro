namespace ArtemisBankingPro.WebApp.ViewModels.Shared;

/// <summary>
/// Pantalla genérica de confirmación previa de operaciones financieras:
/// pregunta exacta del spec, detalle (dl abp-confirm-detail) y botones Cancelar/Confirmar.
/// </summary>
public class ConfirmarOperacionViewModel
{
    public string Titulo { get; set; } = "Confirmación";

    /// <summary>Texto de pregunta EXACTO del spec (Mensajes.Confirmar*).</summary>
    public string Pregunta { get; set; } = null!;

    /// <summary>Advertencia adicional (p. ej. alto riesgo en préstamos).</summary>
    public string? Advertencia { get; set; }

    /// <summary>Pares etiqueta/valor a mostrar en el detalle.</summary>
    public List<KeyValuePair<string, string>> Detalles { get; set; } = [];

    /// <summary>Acción/controlador que ejecuta la operación (POST).</summary>
    public string Accion { get; set; } = null!;
    public string Controlador { get; set; } = null!;

    /// <summary>Campos ocultos que viajan al POST de ejecución.</summary>
    public Dictionary<string, string> Campos { get; set; } = [];

    public string TextoBoton { get; set; } = "Confirmar";

    /// <summary>Destino del botón Cancelar.</summary>
    public string CancelarAccion { get; set; } = "Index";
    public string? CancelarControlador { get; set; }

    public void Agregar(string etiqueta, string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor))
            Detalles.Add(new KeyValuePair<string, string>(etiqueta, valor));
    }
}
