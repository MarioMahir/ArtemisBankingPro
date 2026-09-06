using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.CreditCards.Commands.CancelCreditCard;

/// <summary>
/// PATCH /api/credit-card/{id}/cancel — cancela la tarjeta SOLO si su deuda es RD$0.00.
/// El historial se conserva; nada se elimina físicamente.
/// </summary>
public class CancelCreditCardCommand : IRequest<ServiceResult>
{
    public int CardId { get; set; }
}

public class CancelCreditCardCommandValidator : AbstractValidator<CancelCreditCardCommand>
{
    public CancelCreditCardCommandValidator()
    {
        RuleFor(x => x.CardId).GreaterThan(0).WithMessage("El identificador de la tarjeta no es válido.");
    }
}

public class CancelCreditCardCommandHandler(ICreditCardService creditCardService)
    : IRequestHandler<CancelCreditCardCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(CancelCreditCardCommand request, CancellationToken cancellationToken) =>
        creditCardService.CancelAsync(request.CardId);
}
