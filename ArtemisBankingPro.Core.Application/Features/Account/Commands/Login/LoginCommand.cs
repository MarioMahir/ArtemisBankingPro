using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Account.Commands.Login;

/// <summary>POST /account/login — autentica contra los roles de la API (Administrador, Comercio) y devuelve el JWT.</summary>
public class LoginCommand : IRequest<ServiceResult<LoginResponse>>
{
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResponse
{
    public string Jwt { get; set; } = null!;
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("El nombre de usuario es requerido.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("La contraseña es requerida.");
    }
}

public class LoginCommandHandler(IAccountService accountService, IJwtService jwtService)
    : IRequestHandler<LoginCommand, ServiceResult<LoginResponse>>
{
    public async Task<ServiceResult<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var auth = await accountService.AuthenticateAsync(request.UserName, request.Password, Roles.ApiRoles);

        if (!auth.Succeeded || auth.Data is null)
        {
            // La API usa sus propios textos: inactivo → 401 con el mensaje exacto del spec;
            // rol no permitido en la API → 403 con el mensaje de acceso denegado de la API.
            var message = auth.Message switch
            {
                Mensajes.CuentaInactiva => Mensajes.ApiCuentaInactiva,
                Mensajes.SinPermisosWeb => Mensajes.ApiAccesoDenegado,
                _ => auth.Message ?? Mensajes.CredencialesInvalidas
            };
            return ServiceResult<LoginResponse>.Fail(message);
        }

        return ServiceResult<LoginResponse>.Ok(new LoginResponse { Jwt = jwtService.GenerateToken(auth.Data) });
    }
}
