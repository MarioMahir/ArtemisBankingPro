using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Commerces;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Commerces.Commands.UpdateCommerce;

/// <summary>
/// PUT /api/commerce/{id} — edita datos del comercio SIN tocar su estado.
/// RNC y correo no pueden pertenecer a otro comercio.
/// </summary>
public class UpdateCommerceCommand : IRequest<ServiceResult>
{
    /// <summary>Se asigna desde la ruta; se ignora cualquier valor del cuerpo.</summary>
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Rnc { get; set; } = null!;
}

public class UpdateCommerceCommandValidator : AbstractValidator<UpdateCommerceCommand>
{
    public UpdateCommerceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador del comercio no es válido.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre del comercio es requerido.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("El teléfono es requerido.");
        RuleFor(x => x.Rnc).NotEmpty().WithMessage("El RNC es requerido.");
    }
}

public class UpdateCommerceCommandHandler(ICommerceService commerceService, IMapper mapper)
    : IRequestHandler<UpdateCommerceCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(UpdateCommerceCommand request, CancellationToken cancellationToken) =>
        commerceService.UpdateAsync(request.Id, mapper.Map<SaveCommerceRequest>(request));
}
