using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Features.Common;
using ArtemisBankingPro.Core.Application.Helpers;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Loans.Commands.CreateLoan;

/// <summary>
/// POST /api/loan — asigna un préstamo (sistema francés, desembolso a la cuenta principal).
/// Si el cliente es o se convertirá en alto riesgo y confirmHighRisk es false, el handler
/// devuelve el conflicto (el controller lo traduce a 409 con riskType y las deudas).
/// </summary>
public class CreateLoanCommand : IRequest<CreateLoanResult>, IActingUserRequest
{
    public string ClientId { get; set; } = null!;
    public decimal CapitalAmount { get; set; }
    public int TermInMonths { get; set; }
    public decimal AnnualInterestRate { get; set; }

    /// <summary>true → el administrador confirma la asignación pese al alto riesgo.</summary>
    public bool ConfirmHighRisk { get; set; }

    /// <summary>Id del administrador autenticado (del JWT); lo asigna el controller.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string ActingUserId { get; set; } = null!;
}

/// <summary>Cuerpo del 409 Conflict por alto riesgo sin confirmar.</summary>
public class HighRiskConflict
{
    public string Message { get; set; } = null!;

    /// <summary>"CurrentHighRisk" o "ProjectedHighRisk".</summary>
    public string RiskType { get; set; } = null!;

    public decimal CurrentDebt { get; set; }
    public decimal ProjectedDebt { get; set; }
    public decimal AverageDebt { get; set; }
}

/// <summary>Resultado del command: o bien un ServiceResult normal, o bien un conflicto de alto riesgo.</summary>
public class CreateLoanResult
{
    public ServiceResult<LoanDto>? Result { get; init; }
    public HighRiskConflict? Conflict { get; init; }

    public static CreateLoanResult From(ServiceResult<LoanDto> result) => new() { Result = result };
    public static CreateLoanResult HighRisk(HighRiskConflict conflict) => new() { Conflict = conflict };
}

public class CreateLoanCommandValidator : AbstractValidator<CreateLoanCommand>
{
    public CreateLoanCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty().WithMessage(Mensajes.DebeSeleccionarCliente);
        RuleFor(x => x.CapitalAmount).GreaterThan(0).WithMessage(Mensajes.MontoPrestamoInvalido);
        RuleFor(x => x.TermInMonths)
            .Must(term => AppConstants.AllowedLoanTerms.Contains(term))
            .WithMessage(Mensajes.PlazoInvalido);
        RuleFor(x => x.AnnualInterestRate).GreaterThanOrEqualTo(0).WithMessage(Mensajes.TasaNegativa);
    }
}

public class CreateLoanCommandHandler(ILoanService loanService, IMapper mapper)
    : IRequestHandler<CreateLoanCommand, CreateLoanResult>
{
    public async Task<CreateLoanResult> Handle(CreateLoanCommand request, CancellationToken cancellationToken)
    {
        var createRequest = mapper.Map<CreateLoanRequest>(request);

        // Evaluación de riesgo (incluye las validaciones de negocio del préstamo).
        var risk = await loanService.EvaluateRiskAsync(createRequest);
        if (!risk.Succeeded || risk.Data is null)
            return CreateLoanResult.From(ServiceResult<LoanDto>.Fail(risk.Message!));

        if (risk.Data.RiskLevel != RiskLevel.None && !request.ConfirmHighRisk)
        {
            return CreateLoanResult.HighRisk(new HighRiskConflict
            {
                Message = risk.Data.WarningMessage!,
                RiskType = risk.Data.RiskLevel.ToString(),
                CurrentDebt = risk.Data.CurrentDebt,
                ProjectedDebt = risk.Data.ProjectedDebt,
                AverageDebt = risk.Data.AverageDebt
            });
        }

        var result = await loanService.CreateLoanAsync(createRequest, request.ActingUserId);
        return CreateLoanResult.From(result);
    }
}
