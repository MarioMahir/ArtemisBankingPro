using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Enums;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace ArtemisBankingPro.Core.Application.Features.Users.Queries.GetUserById;

/// <summary>GET /api/users/{id} — detalle del usuario incluyendo su cuenta principal (mainAccount).</summary>
public class GetUserByIdQuery : IRequest<ServiceResult<UserWithMainAccountDto>>
{
    public string Id { get; set; } = null!;
}

/// <summary>Usuario + su cuenta de ahorro principal activa (null si no posee).</summary>
public class UserWithMainAccountDto : UserDto
{
    public SavingsAccountDto? MainAccount { get; set; }
}

public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("El identificador de usuario es requerido.");
    }
}

public class GetUserByIdQueryHandler(
    IUserService userService,
    ISavingsAccountService savingsAccountService,
    IMapper mapper)
    : IRequestHandler<GetUserByIdQuery, ServiceResult<UserWithMainAccountDto>>
{
    public async Task<ServiceResult<UserWithMainAccountDto>> Handle(
        GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await userService.GetByIdAsync(request.Id);
        if (user is null)
            return ServiceResult<UserWithMainAccountDto>.Fail(Mensajes.UsuarioNoExiste);

        var dto = mapper.Map<UserWithMainAccountDto>(user);

        var accounts = await savingsAccountService.GetActiveAccountsByUserAsync(user.Id);
        dto.MainAccount = accounts.FirstOrDefault(a => a.Type == AccountType.Principal);

        return ServiceResult<UserWithMainAccountDto>.Ok(dto);
    }
}
