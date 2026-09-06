using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Commerces.Queries.GetCommerces;

/// <summary>
/// GET /api/commerce — listado paginado (20). Filtro status: "activos" (default),
/// "inactivos" o "todos". Incluye hasAssociatedUser (máx. 1 usuario por comercio).
/// </summary>
public class GetCommercesQuery : IRequest<PagedResult<CommerceApiDto>>
{
    public int Page { get; set; } = 1;

    /// <summary>activos | inactivos | todos (default: activos).</summary>
    public string? Status { get; set; }
}

/// <summary>Comercio para los listados de la API, con el indicador de usuario asociado.</summary>
public class CommerceApiDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Rnc { get; set; } = null!;
    public bool IsActive { get; set; }
    public bool HasAssociatedUser { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetCommercesQueryValidator : AbstractValidator<GetCommercesQuery>
{
    private static readonly string[] AllowedStatuses = ["activos", "inactivos", "todos"];

    public GetCommercesQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || AllowedStatuses.Contains(s.ToLowerInvariant()))
            .WithMessage("El filtro de estado no es válido. Valores permitidos: activos, inactivos, todos.");
    }
}

public class GetCommercesQueryHandler(
    ICommerceService commerceService,
    IAccountService accountService,
    IMapper mapper)
    : IRequestHandler<GetCommercesQuery, PagedResult<CommerceApiDto>>
{
    public async Task<PagedResult<CommerceApiDto>> Handle(GetCommercesQuery request, CancellationToken cancellationToken)
    {
        bool? isActive = request.Status?.ToLowerInvariant() switch
        {
            "inactivos" => false,
            "todos" => null,
            _ => true // default: activos
        };

        var paged = await commerceService.GetPagedAsync(request.Page, isActive);

        var items = new List<CommerceApiDto>(paged.Items.Count);
        foreach (var commerce in paged.Items)
        {
            var dto = mapper.Map<CommerceApiDto>(commerce);
            dto.HasAssociatedUser = await accountService.GetByCommerceIdAsync(commerce.Id) is not null;
            items.Add(dto);
        }

        return new PagedResult<CommerceApiDto>
        {
            Items = items,
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };
    }
}
