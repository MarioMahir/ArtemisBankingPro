using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Loans.Queries.GetLoanById;

/// <summary>GET /api/loan/{id} — detalle del préstamo con su tabla de amortización (amortization[]).</summary>
public class GetLoanByIdQuery : IRequest<ServiceResult<LoanDto>>
{
    public int Id { get; set; }
}

public class GetLoanByIdQueryValidator : AbstractValidator<GetLoanByIdQuery>
{
    public GetLoanByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador del préstamo no es válido.");
    }
}

public class GetLoanByIdQueryHandler(ILoanService loanService)
    : IRequestHandler<GetLoanByIdQuery, ServiceResult<LoanDto>>
{
    public Task<ServiceResult<LoanDto>> Handle(GetLoanByIdQuery request, CancellationToken cancellationToken) =>
        loanService.GetDetailAsync(request.Id);
}
