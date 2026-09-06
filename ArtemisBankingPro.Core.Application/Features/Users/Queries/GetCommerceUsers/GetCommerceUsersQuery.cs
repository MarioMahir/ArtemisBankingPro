using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Users.Queries.GetCommerceUsers;

/// <summary>GET /api/users/commerce — listado paginado (20) SOLO de usuarios con rol Comercio.</summary>
public class GetCommerceUsersQuery : IRequest<PagedResult<UserDto>>
{
    public int Page { get; set; } = 1;
}

public class GetCommerceUsersQueryValidator : AbstractValidator<GetCommerceUsersQuery>
{
    public GetCommerceUsersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");
    }
}

public class GetCommerceUsersQueryHandler(IUserService userService)
    : IRequestHandler<GetCommerceUsersQuery, PagedResult<UserDto>>
{
    public Task<PagedResult<UserDto>> Handle(GetCommerceUsersQuery request, CancellationToken cancellationToken) =>
        userService.GetPagedAsync(request.Page, role: null, onlyCommerce: true);
}
