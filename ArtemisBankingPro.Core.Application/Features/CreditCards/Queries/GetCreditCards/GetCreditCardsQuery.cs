using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.CreditCards.Queries.GetCreditCards;

/// <summary>
/// GET /api/credit-card — listado paginado (20) con el número SIEMPRE enmascarado
/// (***********1234). Filtro status: "activas" (default), "canceladas" o "todas".
/// </summary>
public class GetCreditCardsQuery : IRequest<ServiceResult<PagedResult<CreditCardDto>>>
{
    public int Page { get; set; } = 1;

    /// <summary>activas | canceladas | todas (default: activas).</summary>
    public string? Status { get; set; }

    /// <summary>Cédula del cliente (búsqueda exacta).</summary>
    public string? Identification { get; set; }
}

public class GetCreditCardsQueryValidator : AbstractValidator<GetCreditCardsQuery>
{
    private static readonly string[] AllowedStatuses = ["activas", "canceladas", "todas"];

    public GetCreditCardsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || AllowedStatuses.Contains(s.ToLowerInvariant()))
            .WithMessage("El filtro de estado no es válido. Valores permitidos: activas, canceladas, todas.");
    }
}

public class GetCreditCardsQueryHandler(ICreditCardService creditCardService)
    : IRequestHandler<GetCreditCardsQuery, ServiceResult<PagedResult<CreditCardDto>>>
{
    public Task<ServiceResult<PagedResult<CreditCardDto>>> Handle(
        GetCreditCardsQuery request, CancellationToken cancellationToken)
    {
        var hasIdentification = !string.IsNullOrWhiteSpace(request.Identification);

        ProductStatus? status = request.Status?.ToLowerInvariant() switch
        {
            "canceladas" => ProductStatus.Cancelada,
            "todas" => null,
            "activas" => ProductStatus.Activa,
            _ => hasIdentification ? null : ProductStatus.Activa // default: activas
        };

        return creditCardService.GetPagedAsync(request.Page, status, request.Identification);
    }
}
