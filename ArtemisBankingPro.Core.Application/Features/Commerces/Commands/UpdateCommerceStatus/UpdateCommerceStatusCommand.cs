using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Commerces.Commands.UpdateCommerceStatus;

/// <summary>
/// PATCH /api/commerce/{id}/status — activa/desactiva el comercio.
/// Desactivar inactiva sus usuarios; reactivar NO los reactiva (deben hacer reset).
/// </summary>
public class UpdateCommerceStatusCommand : IRequest<ServiceResult>
{
    /// <summary>Se asigna desde la ruta; se ignora cualquier valor del cuerpo.</summary>
    public int Id { get; set; }

    public bool IsActive { get; set; }
}

public class UpdateCommerceStatusCommandValidator : AbstractValidator<UpdateCommerceStatusCommand>
{
    public UpdateCommerceStatusCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador del comercio no es válido.");
    }
}

public class UpdateCommerceStatusCommandHandler(ICommerceService commerceService)
    : IRequestHandler<UpdateCommerceStatusCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(UpdateCommerceStatusCommand request, CancellationToken cancellationToken) =>
        commerceService.SetStatusAsync(request.Id, request.IsActive);
}
