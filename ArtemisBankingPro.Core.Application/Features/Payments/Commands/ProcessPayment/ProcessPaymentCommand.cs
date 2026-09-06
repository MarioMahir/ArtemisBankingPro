using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Payments.Commands.ProcessPayment;

/// <summary>
/// POST /pay/process-payment/{commerceId} — Hermes Pay. Valida tarjeta (existe, activa,
/// no vencida, CVC contra hash), comercio (activo, con usuario y principal activa) y crédito
/// disponible. Aprobado → transaccional (deuda + consumo + CRÉDITO al comercio); rechazo por
/// crédito → consumo RECHAZADO sin tocar balances.
/// </summary>
public class ProcessPaymentCommand : IRequest<ServiceResult>
{
    /// <summary>Efectivo: lo resuelve el controller (rol Comercio → JWT; Administrador → URL).</summary>
    public int CommerceId { get; set; }

    /// <summary>Número completo de tarjeta: 16 dígitos.</summary>
    public string CardNumber { get; set; } = null!;

    /// <summary>Mes de expiración "MM" (01-12).</summary>
    public string MonthExpirationCard { get; set; } = null!;

    /// <summary>Año de expiración "YYYY".</summary>
    public string YearExpirationCard { get; set; } = null!;

    /// <summary>CVC de 3 dígitos (se compara contra el hash; jamás se almacena ni loguea).</summary>
    public string Cvc { get; set; } = null!;

    public decimal TransactionAmount { get; set; }
}

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.CommerceId).GreaterThan(0).WithMessage("El identificador del comercio no es válido.");

        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("El número de tarjeta es requerido.")
            .Matches($"^\\d{{{AppConstants.CardNumberLength}}}$")
            .WithMessage("El número de tarjeta debe tener 16 dígitos.");

        RuleFor(x => x.MonthExpirationCard)
            .NotEmpty().WithMessage("El mes de expiración es requerido.")
            .Matches("^(0[1-9]|1[0-2])$").WithMessage("El mes de expiración debe tener el formato MM (01-12).");

        RuleFor(x => x.YearExpirationCard)
            .NotEmpty().WithMessage("El año de expiración es requerido.")
            .Matches("^\\d{4}$").WithMessage("El año de expiración debe tener el formato YYYY.");

        RuleFor(x => x.Cvc)
            .NotEmpty().WithMessage("El CVC es requerido.")
            .Matches($"^\\d{{{AppConstants.CvcLength}}}$").WithMessage("El CVC debe tener 3 dígitos.");

        RuleFor(x => x.TransactionAmount)
            .GreaterThan(0).WithMessage("El monto de la transacción debe ser mayor que cero.");
    }
}

public class ProcessPaymentCommandHandler(IHermesPayService hermesPayService)
    : IRequestHandler<ProcessPaymentCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken) =>
        hermesPayService.ProcessPaymentAsync(
            request.CommerceId,
            request.CardNumber,
            request.MonthExpirationCard,
            request.YearExpirationCard,
            request.Cvc,
            request.TransactionAmount);
}
