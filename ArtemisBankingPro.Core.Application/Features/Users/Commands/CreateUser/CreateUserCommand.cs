using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// POST /api/users — crea un usuario Administrador, Cajero o Cliente. El usuario nace
/// Inactivo y recibe el token de activación EN EL CUERPO del correo (regla de la API).
/// Cliente → cuenta de ahorro principal automática con el monto inicial.
/// </summary>
public class CreateUserCommand : IRequest<ServiceResult<UserDto>>
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Identification { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;

    /// <summary>Administrador, Cajero o Cliente (los usuarios Comercio se crean por su endpoint propio).</summary>
    public string Role { get; set; } = null!;

    /// <summary>Solo aplica a Cliente. Vacío → RD$0.00.</summary>
    public decimal? InitialAmount { get; set; }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("El nombre es requerido.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("El apellido es requerido.");
        RuleFor(x => x.Identification).NotEmpty().WithMessage("La cédula es requerida.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.");
        RuleFor(x => x.UserName).NotEmpty().WithMessage("El nombre de usuario es requerido.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("La contraseña es requerida.");
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("La confirmación de contraseña es requerida.")
            .Equal(x => x.Password).WithMessage(Mensajes.ContrasenasNoCoinciden);

        RuleFor(x => x.Role)
            .Must(role => Roles.WebAppRoles.Contains(role))
            .WithMessage("El tipo de usuario no es válido. Valores permitidos: Administrador, Cajero, Cliente.");

        RuleFor(x => x.InitialAmount)
            .GreaterThanOrEqualTo(0).WithMessage(Mensajes.MontoInicialNegativo)
            .When(x => x.InitialAmount.HasValue);

        RuleFor(x => x.InitialAmount)
            .Null().WithMessage("El monto inicial solo aplica a usuarios de tipo Cliente.")
            .When(x => x.Role != Roles.Cliente);
    }
}

public class CreateUserCommandHandler(IUserService userService, IMapper mapper)
    : IRequestHandler<CreateUserCommand, ServiceResult<UserDto>>
{
    public Task<ServiceResult<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var createRequest = mapper.Map<CreateUserRequest>(request);
        return userService.CreateUserAsync(createRequest);
    }
}
