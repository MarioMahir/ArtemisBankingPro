using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Loans.Commands.UpdateLoanRate;

/// <summary>
/// PATCH /api/loan/{id}/rate — cambia la tasa anual recalculando SOLO las cuotas
/// pendientes con vencimiento futuro (nunca pagadas, parciales ni vencidas).
/// </summary>
public class UpdateLoanRateCommand : IRequest<ServiceResult>
{
    /// <summary>Se asigna desde la ruta; se ignora cualquier valor del cuerpo.</summary>
    public int LoanId { get; set; }

    public decimal AnnualInterestRate { get; set; }
}

public class UpdateLoanRateCommandValidator : AbstractValidator<UpdateLoanRateCommand>
{
    public UpdateLoanRateCommandValidator()
    {
        RuleFor(x => x.LoanId).GreaterThan(0).WithMessage("El identificador del préstamo no es válido.");
        RuleFor(x => x.AnnualInterestRate).GreaterThanOrEqualTo(0).WithMessage(Mensajes.TasaNegativa);
    }
}

public class UpdateLoanRateCommandHandler(ILoanService loanService)
    : IRequestHandler<UpdateLoanRateCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(UpdateLoanRateCommand request, CancellationToken cancellationToken) =>
        loanService.UpdateRateAsync(request.LoanId, request.AnnualInterestRate);
}
