using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Application.Features.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.SavingsAccounts.Commands.CreateSavingsAccount;

/// <summary>
/// POST /api/savings-account — asigna una cuenta SECUNDARIA (solo secundarias por la API)
/// a un cliente activo con principal activa. Balance inicial ≥ 0; &gt; 0 → transacción CRÉDITO.
/// </summary>
public class CreateSavingsAccountCommand : IRequest<ServiceResult<SavingsAccountDto>>, IActingUserRequest
{
    public string ClientId { get; set; } = null!;
    public decimal? InitialBalance { get; set; }

    /// <summary>Id del administrador autenticado (del JWT); lo asigna el controller.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string ActingUserId { get; set; } = null!;
}

public class CreateSavingsAccountCommandValidator : AbstractValidator<CreateSavingsAccountCommand>
{
    public CreateSavingsAccountCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("El identificador del cliente es requerido.");

        RuleFor(x => x.InitialBalance)
            .NotNull().WithMessage("El balance inicial es requerido.")
            .GreaterThanOrEqualTo(0).WithMessage(Mensajes.BalanceInicialNegativo);
    }
}

public class CreateSavingsAccountCommandHandler(ISavingsAccountService savingsAccountService)
    : IRequestHandler<CreateSavingsAccountCommand, ServiceResult<SavingsAccountDto>>
{
    public Task<ServiceResult<SavingsAccountDto>> Handle(
        CreateSavingsAccountCommand request, CancellationToken cancellationToken) =>
        savingsAccountService.CreateSecondaryAsync(request.ClientId, request.InitialBalance, request.ActingUserId);
}
