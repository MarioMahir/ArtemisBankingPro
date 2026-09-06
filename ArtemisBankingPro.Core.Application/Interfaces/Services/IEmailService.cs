using ArtemisBankingPro.Core.Application.Dtos.Email;

namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

public interface IEmailService
{
    /// <summary>
    /// Devuelve false si el envío falla. Un fallo de correo NUNCA revierte la
    /// operación de negocio: el llamador decide qué mensaje mostrar.
    /// </summary>
    Task<bool> SendAsync(EmailRequest request);
}
