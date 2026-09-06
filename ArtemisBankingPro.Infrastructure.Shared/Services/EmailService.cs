using ArtemisBankingPro.Core.Application.Dtos.Email;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Infrastructure.Shared.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ArtemisBankingPro.Infrastructure.Shared.Services;

/// <summary>
/// Servicio de correo compartido por la WebApp y la WebAPI. Con SMTP configurado
/// envía por MailKit; sin usuario configurado, guarda cada correo como archivo HTML
/// para poder probar activaciones, restablecimientos y notificaciones en desarrollo.
/// </summary>
public class EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = settings.Value;

    public async Task<bool> SendAsync(EmailRequest request)
    {
        try
        {
            if (!_settings.IsSmtpConfigured)
            {
                await SaveToFileAsync(request);
                return true;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(request.To));
            message.Subject = request.Subject;
            message.Body = new BodyBuilder { HtmlBody = request.HtmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            // El fallo se registra pero jamás revierte la operación de negocio que originó el correo.
            logger.LogError(ex, "No fue posible enviar el correo con asunto {Subject}", request.Subject);
            return false;
        }
    }

    private async Task SaveToFileAsync(EmailRequest request)
    {
        var folder = Path.Combine(Directory.GetCurrentDirectory(), _settings.OutputFolder);
        Directory.CreateDirectory(folder);

        var safeTo = string.Concat(request.To.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var path = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd-HHmmss-fff}_{safeTo}.html");

        var html = $"""
            <!DOCTYPE html>
            <html lang="es"><head><meta charset="utf-8"><title>{request.Subject}</title></head>
            <body style="font-family:Segoe UI,Arial,sans-serif;max-width:640px;margin:24px auto;padding:0 16px">
            <div style="background:#f1f3f5;border-radius:8px;padding:12px 16px;font-size:13px;color:#495057">
              <strong>Correo simulado (sin SMTP configurado)</strong><br>
              Para: {request.To}<br>
              Asunto: {request.Subject}<br>
              Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}
            </div>
            <hr>
            {request.HtmlBody}
            </body></html>
            """;

        await File.WriteAllTextAsync(path, html);

        logger.LogWarning("SMTP no configurado. Correo para {To} guardado en {Path}", request.To, path);
    }
}
