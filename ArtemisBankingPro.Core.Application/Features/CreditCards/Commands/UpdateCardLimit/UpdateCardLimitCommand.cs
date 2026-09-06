using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.CreditCards.Commands.UpdateCardLimit;

/// <summary>
/// PATCH /api/credit-card/{id}/limit — nuevo límite: tarjeta activa, mayor que cero
/// y nunca inferior a la deuda actual.
/// </summary>
public class UpdateCardLimitCommand : IRequest<ServiceResult>
{
    /// <summary>Se asigna desde la ruta; se ignora cualquier valor del cuerpo.</summary>
    public int CardId { get; set; }

    public decimal NewLimit { get; set; }
}

public class UpdateCardLimitCommandValidator : AbstractValidator<UpdateCardLimitCommand>
{
    public UpdateCardLimitCommandValidator()
    {
        RuleFor(x => x.CardId).GreaterThan(0).WithMessage("El identificador de la tarjeta no es válido.");
        RuleFor(x => x.NewLimit).GreaterThan(0).WithMessage(Mensajes.LimiteTarjetaInvalido);
    }
}

public class UpdateCardLimitCommandHandler(ICreditCardService creditCardService)
    : IRequestHandler<UpdateCardLimitCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(UpdateCardLimitCommand request, CancellationToken cancellationToken) =>
        creditCardService.UpdateLimitAsync(request.CardId, request.NewLimit);
}
