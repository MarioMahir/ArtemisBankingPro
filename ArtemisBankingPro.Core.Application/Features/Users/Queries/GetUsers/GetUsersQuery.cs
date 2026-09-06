using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// GET /api/users — listado paginado (20) de usuarios de la WebApp, EXCLUYE el rol
/// Comercio, más recientes primero, con filtro opcional por rol.
/// </summary>
public class GetUsersQuery : IRequest<PagedResult<UserDto>>
{
    public int Page { get; set; } = 1;
    public string? Role { get; set; }
}

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");

        // El spec usa valores en minúscula (administrador|cajero|cliente): se acepta sin distinguir mayúsculas.
        RuleFor(x => x.Role)
            .Must(role => string.IsNullOrWhiteSpace(role)
                || Roles.WebAppRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage("El rol de filtro no es válido. Valores permitidos: administrador, cajero, cliente.");
    }
}

public class GetUsersQueryHandler(IUserService userService)
    : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    public Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        // Normaliza al nombre exacto del rol de Identity.
        var role = Roles.WebAppRoles.FirstOrDefault(r =>
            string.Equals(r, request.Role, StringComparison.OrdinalIgnoreCase));
        return userService.GetPagedAsync(request.Page, role);
    }
}
