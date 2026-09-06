using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.SavingsAccounts.Commands.CancelSavingsAccount;

/// <summary>
/// PATCH /api/savings-account/{accountNumber}/cancel — cancela una cuenta SECUNDARIA
/// transfiriendo su balance a la principal (DÉBITO + CRÉDITO, transaccional).
/// Las cuentas principales JAMÁS se cancelan.
/// </summary>
public class CancelSavingsAccountCommand : IRequest<ServiceResult>
{
    public string AccountNumber { get; set; } = null!;
}

public class CancelSavingsAccountCommandValidator : AbstractValidator<CancelSavingsAccountCommand>
{
    public CancelSavingsAccountCommandValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("El número de cuenta es requerido.")
            .Matches($"^\\d{{{AppConstants.AccountNumberLength}}}$")
            .WithMessage(Mensajes.CuentaInvalida);
    }
}

public class CancelSavingsAccountCommandHandler(ISavingsAccountService savingsAccountService)
    : IRequestHandler<CancelSavingsAccountCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(CancelSavingsAccountCommand request, CancellationToken cancellationToken) =>
        savingsAccountService.CancelAsync(request.AccountNumber);
}
