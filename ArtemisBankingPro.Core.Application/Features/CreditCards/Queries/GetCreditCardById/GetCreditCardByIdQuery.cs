using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.CreditCards.Queries.GetCreditCardById;

/// <summary>GET /api/credit-card/{id} — detalle de la tarjeta con sus consumos (consumptions[]).</summary>
public class GetCreditCardByIdQuery : IRequest<ServiceResult<CreditCardDto>>
{
    public int Id { get; set; }
}

public class GetCreditCardByIdQueryValidator : AbstractValidator<GetCreditCardByIdQuery>
{
    public GetCreditCardByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador de la tarjeta no es válido.");
    }
}

public class GetCreditCardByIdQueryHandler(ICreditCardService creditCardService)
    : IRequestHandler<GetCreditCardByIdQuery, ServiceResult<CreditCardDto>>
{
    public Task<ServiceResult<CreditCardDto>> Handle(GetCreditCardByIdQuery request, CancellationToken cancellationToken) =>
        creditCardService.GetDetailAsync(request.Id);
}
