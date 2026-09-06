using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Commerces;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Commerces.Commands.CreateCommerce;

/// <summary>
/// POST /api/commerce — registra un comercio (RNC y correo únicos).
/// El comercio nace Activo y SIN usuario asociado.
/// </summary>
public class CreateCommerceCommand : IRequest<ServiceResult<CommerceDto>>
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Rnc { get; set; } = null!;
}

public class CreateCommerceCommandValidator : AbstractValidator<CreateCommerceCommand>
{
    public CreateCommerceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre del comercio es requerido.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("El teléfono es requerido.");
        RuleFor(x => x.Rnc).NotEmpty().WithMessage("El RNC es requerido.");
    }
}

public class CreateCommerceCommandHandler(ICommerceService commerceService, IMapper mapper)
    : IRequestHandler<CreateCommerceCommand, ServiceResult<CommerceDto>>
{
    public Task<ServiceResult<CommerceDto>> Handle(CreateCommerceCommand request, CancellationToken cancellationToken) =>
        commerceService.CreateAsync(mapper.Map<SaveCommerceRequest>(request));
}
