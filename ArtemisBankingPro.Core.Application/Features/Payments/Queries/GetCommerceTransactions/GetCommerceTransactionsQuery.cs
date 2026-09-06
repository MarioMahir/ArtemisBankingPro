using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Payments.Queries.GetCommerceTransactions;

/// <summary>
/// GET /pay/get-transactions/{commerceId} — consumos del comercio (Hermes Pay), paginados (20),
/// más recientes primero. El commerceId efectivo lo resuelve el controller: rol Comercio →
/// claim del JWT (se ignora la URL); Administrador → URL.
/// </summary>
public class GetCommerceTransactionsQuery : IRequest<ServiceResult<PagedResult<ConsumptionDto>>>
{
    public int CommerceId { get; set; }
    public int Page { get; set; } = 1;
}

public class GetCommerceTransactionsQueryValidator : AbstractValidator<GetCommerceTransactionsQuery>
{
    public GetCommerceTransactionsQueryValidator()
    {
        RuleFor(x => x.CommerceId).GreaterThan(0).WithMessage("El identificador del comercio no es válido.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");
    }
}

public class GetCommerceTransactionsQueryHandler(IHermesPayService hermesPayService)
    : IRequestHandler<GetCommerceTransactionsQuery, ServiceResult<PagedResult<ConsumptionDto>>>
{
    public Task<ServiceResult<PagedResult<ConsumptionDto>>> Handle(
        GetCommerceTransactionsQuery request, CancellationToken cancellationToken) =>
        hermesPayService.GetTransactionsAsync(request.CommerceId, request.Page);
}
