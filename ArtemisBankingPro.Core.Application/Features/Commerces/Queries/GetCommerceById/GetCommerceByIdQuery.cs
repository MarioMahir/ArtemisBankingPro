using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Features.Commerces.Queries.GetCommerces;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Commerces.Queries.GetCommerceById;

/// <summary>GET /api/commerce/{id} — detalle del comercio con su usuario asociado (associatedUser).</summary>
public class GetCommerceByIdQuery : IRequest<ServiceResult<CommerceDetailDto>>
{
    public int Id { get; set; }
}

/// <summary>Comercio + su usuario asociado (null si aún no lo tiene).</summary>
public class CommerceDetailDto : CommerceApiDto
{
    public UserDto? AssociatedUser { get; set; }
}

public class GetCommerceByIdQueryValidator : AbstractValidator<GetCommerceByIdQuery>
{
    public GetCommerceByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El identificador del comercio no es válido.");
    }
}

public class GetCommerceByIdQueryHandler(
    ICommerceService commerceService,
    IAccountService accountService,
    IMapper mapper)
    : IRequestHandler<GetCommerceByIdQuery, ServiceResult<CommerceDetailDto>>
{
    public async Task<ServiceResult<CommerceDetailDto>> Handle(
        GetCommerceByIdQuery request, CancellationToken cancellationToken)
    {
        var commerce = await commerceService.GetByIdAsync(request.Id);
        if (!commerce.Succeeded || commerce.Data is null)
            return ServiceResult<CommerceDetailDto>.Fail(commerce.Message!);

        var dto = mapper.Map<CommerceDetailDto>(commerce.Data);
        dto.AssociatedUser = await accountService.GetByCommerceIdAsync(request.Id);
        dto.HasAssociatedUser = dto.AssociatedUser is not null;

        return ServiceResult<CommerceDetailDto>.Ok(dto);
    }
}
