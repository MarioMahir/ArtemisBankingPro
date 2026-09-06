using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Features.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// PUT /api/users/{id} — edita datos del usuario SIN cambiar su rol. Contraseña opcional.
/// AdditionalAmount &gt; 0 → CRÉDITO a la cuenta principal (Cliente y Comercio).
/// El administrador autenticado no puede editarse a sí mismo.
/// </summary>
public class UpdateUserCommand : IRequest<ServiceResult>, IActingUserRequest
{
    /// <summary>Se asigna desde la ruta; se ignora cualquier valor del cuerpo.</summary>
    public string Id { get; set; } = null!;

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Identification { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;

    /// <summary>Opcional: vacía → la contraseña actual no cambia.</summary>
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }

    /// <summary>Monto adicional ≥ 0 que se SUMA al balance de la cuenta principal (CRÉDITO).</summary>
    public decimal? AdditionalAmount { get; set; }

    /// <summary>Id del administrador autenticado (del JWT); lo asigna el controller.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string ActingUserId { get; set; } = null!;
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El identificador de usuario es requerido.");
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("El nombre es requerido.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("El apellido es requerido.");
        RuleFor(x => x.Identification).NotEmpty().WithMessage("La cédula es requerida.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.");
        RuleFor(x => x.UserName).NotEmpty().WithMessage("El nombre de usuario es requerido.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage(Mensajes.ContrasenasNoCoinciden)
            .When(x => !string.IsNullOrWhiteSpace(x.Password));

        RuleFor(x => x.AdditionalAmount)
            .GreaterThanOrEqualTo(0).WithMessage(Mensajes.MontoInicialNegativo)
            .When(x => x.AdditionalAmount.HasValue);
    }
}

public class UpdateUserCommandHandler(IUserService userService, IMapper mapper)
    : IRequestHandler<UpdateUserCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var updateRequest = mapper.Map<UpdateUserRequest>(request);
        return userService.UpdateUserAsync(updateRequest, request.ActingUserId);
    }
}
