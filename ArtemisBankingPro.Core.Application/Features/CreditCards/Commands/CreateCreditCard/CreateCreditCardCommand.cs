using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Features.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.CreditCards.Commands.CreateCreditCard;

/// <summary>
/// POST /api/credit-card — asigna una tarjeta a un cliente activo: nº de 16 dígitos único,
/// deuda inicial RD$0.00, expiración hoy + 3 años, CVC solo hash SHA-256.
/// </summary>
public class CreateCreditCardCommand : IRequest<ServiceResult<CreditCardDto>>, IActingUserRequest
{
    public string ClientId { get; set; } = null!;
    public decimal CreditLimit { get; set; }

    /// <summary>Id del administrador autenticado (del JWT); lo asigna el controller.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string ActingUserId { get; set; } = null!;
}

public class CreateCreditCardCommandValidator : AbstractValidator<CreateCreditCardCommand>
{
    public CreateCreditCardCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("El identificador del cliente es requerido.");
        RuleFor(x => x.CreditLimit).GreaterThan(0).WithMessage(Mensajes.LimiteInvalido);
    }
}

public class CreateCreditCardCommandHandler(ICreditCardService creditCardService)
    : IRequestHandler<CreateCreditCardCommand, ServiceResult<CreditCardDto>>
{
    public Task<ServiceResult<CreditCardDto>> Handle(CreateCreditCardCommand request, CancellationToken cancellationToken) =>
        creditCardService.AssignAsync(request.ClientId, request.CreditLimit, request.ActingUserId);
}
