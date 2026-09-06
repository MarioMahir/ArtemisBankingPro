using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Loans.Queries.GetLoans;

/// <summary>
/// GET /api/loan — listado paginado (20). Filtro status: "activos" (default),
/// "completados" o "todos". Búsqueda por cédula; por cédula sin filtro de estado:
/// activos primero, luego completados, cada grupo más reciente primero.
/// </summary>
public class GetLoansQuery : IRequest<ServiceResult<PagedResult<LoanDto>>>
{
    public int Page { get; set; } = 1;

    /// <summary>activos | completados | todos (default: activos).</summary>
    public string? Status { get; set; }

    /// <summary>Cédula del cliente (búsqueda exacta).</summary>
    public string? Identification { get; set; }
}

public class GetLoansQueryValidator : AbstractValidator<GetLoansQuery>
{
    private static readonly string[] AllowedStatuses = ["activos", "completados", "todos"];

    public GetLoansQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || AllowedStatuses.Contains(s.ToLowerInvariant()))
            .WithMessage("El filtro de estado no es válido. Valores permitidos: activos, completados, todos.");
    }
}

public class GetLoansQueryHandler(ILoanService loanService)
    : IRequestHandler<GetLoansQuery, ServiceResult<PagedResult<LoanDto>>>
{
    public Task<ServiceResult<PagedResult<LoanDto>>> Handle(GetLoansQuery request, CancellationToken cancellationToken)
    {
        // "todos" y una cédula sin filtro explícito → sin filtro de estado (null);
        // en ese caso el servicio ordena activos primero, luego completados.
        var hasIdentification = !string.IsNullOrWhiteSpace(request.Identification);

        LoanStatus? status = request.Status?.ToLowerInvariant() switch
        {
            "completados" => LoanStatus.Completado,
            "todos" => null,
            "activos" => LoanStatus.Activo,
            _ => hasIdentification ? null : LoanStatus.Activo // default: activos
        };

        return loanService.GetPagedAsync(request.Page, status, request.Identification);
    }
}
