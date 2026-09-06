using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Features.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Users.Commands.UpdateUserStatus;

/// <summary>
/// PATCH /api/users/{id}/status — activa/inactiva al usuario. El administrador
/// autenticado (id del JWT) NO puede modificar su propio estado.
/// </summary>
public class UpdateUserStatusCommand : IRequest<ServiceResult>, IActingUserRequest
{
    /// <summary>Se asigna desde la ruta; se ignora cualquier valor del cuerpo.</summary>
    public string UserId { get; set; } = null!;

    public bool IsActive { get; set; }

    /// <summary>Id del administrador autenticado (del JWT); lo asigna el controller.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string ActingUserId { get; set; } = null!;
}

public class UpdateUserStatusCommandValidator : AbstractValidator<UpdateUserStatusCommand>
{
    public UpdateUserStatusCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("El identificador de usuario es requerido.");
    }
}

public class UpdateUserStatusCommandHandler(IUserService userService)
    : IRequestHandler<UpdateUserStatusCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken) =>
        userService.SetUserStatusAsync(request.UserId, request.IsActive, request.ActingUserId);
}
