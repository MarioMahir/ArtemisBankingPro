using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Users.Commands.CreateCommerceUser;

/// <summary>
/// POST /api/users/commerce/{commerceId} — crea el usuario (rol Comercio) del comercio.
/// Reglas: el comercio debe existir y NO tener otro usuario asociado (máx. 1);
/// initialAmount es REQUERIDO; se crea la cuenta de ahorro principal automática.
/// </summary>
public class CreateCommerceUserCommand : IRequest<ServiceResult<UserDto>>
{
    /// <summary>Mensaje del conflicto 409 cuando el comercio ya tiene su usuario (máximo uno).</summary>
    public const string ComercioYaTieneUsuario = "El comercio seleccionado ya tiene un usuario asociado.";

    /// <summary>Se asigna desde la ruta; se ignora cualquier valor del cuerpo.</summary>
    public int CommerceId { get; set; }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Identification { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;

    /// <summary>REQUERIDO para usuarios de comercio (balance de la cuenta principal automática).</summary>
    public decimal? InitialAmount { get; set; }
}

public class CreateCommerceUserCommandValidator : AbstractValidator<CreateCommerceUserCommand>
{
    public CreateCommerceUserCommandValidator()
    {
        RuleFor(x => x.CommerceId)
            .GreaterThan(0).WithMessage("El identificador del comercio no es válido.");
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

        RuleFor(x => x.InitialAmount)
            .NotNull().WithMessage("El monto inicial es requerido para usuarios de comercio.")
            .GreaterThanOrEqualTo(0).WithMessage(Mensajes.MontoInicialNegativo);
    }
}

public class CreateCommerceUserCommandHandler(
    IUserService userService,
    IAccountService accountService,
    ICommerceService commerceService,
    IMapper mapper)
    : IRequestHandler<CreateCommerceUserCommand, ServiceResult<UserDto>>
{
    public async Task<ServiceResult<UserDto>> Handle(
        CreateCommerceUserCommand request, CancellationToken cancellationToken)
    {
        // Reglas de negocio con datos: el comercio existe y aún no tiene usuario.
        var commerce = await commerceService.GetByIdAsync(request.CommerceId);
        if (!commerce.Succeeded)
            return ServiceResult<UserDto>.Fail(Mensajes.ComercioNoExiste);

        var existingUser = await accountService.GetByCommerceIdAsync(request.CommerceId);
        if (existingUser is not null)
            return ServiceResult<UserDto>.Fail(CreateCommerceUserCommand.ComercioYaTieneUsuario);

        var createRequest = mapper.Map<CreateUserRequest>(request);
        return await userService.CreateUserAsync(createRequest);
    }
}
