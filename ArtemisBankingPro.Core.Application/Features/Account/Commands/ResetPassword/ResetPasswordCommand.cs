using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Account.Commands.ResetPassword;

/// <summary>
/// POST /account/reset-password — valida el token (un solo uso, vigencia 30 min),
/// cambia la contraseña y reactiva la cuenta.
/// </summary>
public class ResetPasswordCommand : IRequest<ServiceResult>
{
    public string UserId { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("El identificador de usuario es requerido.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("El token de restablecimiento es requerido.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("La contraseña es requerida.");
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("La confirmación de contraseña es requerida.")
            .Equal(x => x.Password).WithMessage(Mensajes.ContrasenasNoCoinciden);
    }
}

public class ResetPasswordCommandHandler(IAccountService accountService)
    : IRequestHandler<ResetPasswordCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken) =>
        accountService.ResetPasswordAsync(request.UserId, request.Token, request.Password, request.ConfirmPassword);
}
