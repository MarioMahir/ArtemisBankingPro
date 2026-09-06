namespace ArtemisBankingPro.Infrastructure.Shared.Settings;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    /// <summary>Usuario SMTP. Vacío = modo desarrollo: los correos se guardan como archivos HTML.</summary>
    public string SmtpUser { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public string FromAddress { get; set; } = "no-reply@artemisbank.local";

    public string FromName { get; set; } = "Artemis Banking Pro";

    /// <summary>Carpeta (relativa a la raíz del host) donde se escriben los correos simulados.</summary>
    public string OutputFolder { get; set; } = "App_Data/correos";

    public bool IsSmtpConfigured =>
        !string.IsNullOrWhiteSpace(SmtpHost) && !string.IsNullOrWhiteSpace(SmtpUser);
}
