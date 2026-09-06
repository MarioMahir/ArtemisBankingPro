using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Account.Commands.ConfirmAccount;

/// <summary>POST /account/confirm — activa la cuenta con el token recibido en el correo (un solo uso).</summary>
public class ConfirmAccountCommand : IRequest<ServiceResult>
{
    public string Token { get; set; } = null!;
}

public class ConfirmAccountCommandValidator : AbstractValidator<ConfirmAccountCommand>
{
    public ConfirmAccountCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("El token de activación es requerido.");
    }
}

public class ConfirmAccountCommandHandler(IAccountService accountService)
    : IRequestHandler<ConfirmAccountCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(ConfirmAccountCommand request, CancellationToken cancellationToken) =>
        accountService.ConfirmAccountAsync(request.Token);
}
