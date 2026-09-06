using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Account.Commands.GetResetToken;

/// <summary>
/// POST /account/get-reset-token — desactiva temporalmente la cuenta, genera el token de
/// restablecimiento y lo envía EN EL CUERPO del correo (tokenInBody: true, regla de la API).
/// </summary>
public class GetResetTokenCommand : IRequest<ServiceResult>
{
    public string UserName { get; set; } = null!;
}

public class GetResetTokenCommandValidator : AbstractValidator<GetResetTokenCommand>
{
    public GetResetTokenCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("El nombre de usuario es requerido.");
    }
}

public class GetResetTokenCommandHandler(IAccountService accountService)
    : IRequestHandler<GetResetTokenCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(GetResetTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await accountService.RequestPasswordResetAsync(
            request.UserName, tokenInBody: true, Roles.ApiRoles);

        // El texto de "sin permisos" del servicio está redactado para la WebApp;
        // en la API se responde con el mensaje de acceso denegado propio.
        if (!result.Succeeded && result.Message == Mensajes.SinPermisosWeb)
            return ServiceResult.Fail(Mensajes.ApiAccesoDenegado);

        return result;
    }
}
