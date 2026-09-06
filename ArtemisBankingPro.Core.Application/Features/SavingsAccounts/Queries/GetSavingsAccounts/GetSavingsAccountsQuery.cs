using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.SavingsAccounts.Queries.GetSavingsAccounts;

/// <summary>
/// GET /api/savings-account — listado paginado (20). Filtro status: "activa" (default),
/// "cancelada" o "todas". Filtro type: "principal", "secundaria" o "todas" (default).
/// </summary>
public class GetSavingsAccountsQuery : IRequest<ServiceResult<PagedResult<SavingsAccountDto>>>
{
    public int Page { get; set; } = 1;

    /// <summary>activa | cancelada | todas (default: activa).</summary>
    public string? Status { get; set; }

    /// <summary>principal | secundaria | todas (default: todas).</summary>
    public string? Type { get; set; }

    /// <summary>Cédula del cliente (búsqueda exacta).</summary>
    public string? Identification { get; set; }
}

public class GetSavingsAccountsQueryValidator : AbstractValidator<GetSavingsAccountsQuery>
{
    private static readonly string[] AllowedStatuses = ["activa", "cancelada", "todas"];
    private static readonly string[] AllowedTypes = ["principal", "secundaria", "todas"];

    public GetSavingsAccountsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || AllowedStatuses.Contains(s.ToLowerInvariant()))
            .WithMessage("El filtro de estado no es válido. Valores permitidos: activa, cancelada, todas.");

        RuleFor(x => x.Type)
            .Must(t => string.IsNullOrWhiteSpace(t) || AllowedTypes.Contains(t.ToLowerInvariant()))
            .WithMessage("El filtro de tipo no es válido. Valores permitidos: principal, secundaria, todas.");
    }
}

public class GetSavingsAccountsQueryHandler(ISavingsAccountService savingsAccountService)
    : IRequestHandler<GetSavingsAccountsQuery, ServiceResult<PagedResult<SavingsAccountDto>>>
{
    public Task<ServiceResult<PagedResult<SavingsAccountDto>>> Handle(
        GetSavingsAccountsQuery request, CancellationToken cancellationToken)
    {
        var hasIdentification = !string.IsNullOrWhiteSpace(request.Identification);

        ProductStatus? status = request.Status?.ToLowerInvariant() switch
        {
            "cancelada" => ProductStatus.Cancelada,
            "todas" => null,
            "activa" => ProductStatus.Activa,
            _ => hasIdentification ? null : ProductStatus.Activa // default: activa
        };

        AccountType? type = request.Type?.ToLowerInvariant() switch
        {
            "principal" => AccountType.Principal,
            "secundaria" => AccountType.Secundaria,
            _ => null // default: todas
        };

        return savingsAccountService.GetPagedAsync(request.Page, status, type, request.Identification);
    }
}
