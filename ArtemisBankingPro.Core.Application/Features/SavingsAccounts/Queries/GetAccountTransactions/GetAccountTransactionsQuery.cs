using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Transactions;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.SavingsAccounts.Queries.GetAccountTransactions;

/// <summary>
/// GET /api/savings-account/{accountNumber}/transactions — transacciones de la cuenta,
/// paginadas (20), más recientes primero.
/// </summary>
public class GetAccountTransactionsQuery : IRequest<ServiceResult<PagedResult<TransactionDto>>>
{
    public string AccountNumber { get; set; } = null!;
    public int Page { get; set; } = 1;
}

public class GetAccountTransactionsQueryValidator : AbstractValidator<GetAccountTransactionsQuery>
{
    public GetAccountTransactionsQueryValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("El número de cuenta es requerido.")
            .Matches($"^\\d{{{AppConstants.AccountNumberLength}}}$")
            .WithMessage(Mensajes.CuentaInvalida);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");
    }
}

public class GetAccountTransactionsQueryHandler(ISavingsAccountService savingsAccountService)
    : IRequestHandler<GetAccountTransactionsQuery, ServiceResult<PagedResult<TransactionDto>>>
{
    public Task<ServiceResult<PagedResult<TransactionDto>>> Handle(
        GetAccountTransactionsQuery request, CancellationToken cancellationToken) =>
        savingsAccountService.GetTransactionsAsync(request.AccountNumber, request.Page);
}
