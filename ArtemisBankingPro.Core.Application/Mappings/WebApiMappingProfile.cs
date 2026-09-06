using ArtemisBankingPro.Core.Application.Dtos.Commerces;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Features.Commerces.Commands.CreateCommerce;
using ArtemisBankingPro.Core.Application.Features.Commerces.Commands.UpdateCommerce;
using ArtemisBankingPro.Core.Application.Features.Commerces.Queries.GetCommerceById;
using ArtemisBankingPro.Core.Application.Features.Commerces.Queries.GetCommerces;
using ArtemisBankingPro.Core.Application.Features.Loans.Commands.CreateLoan;
using ArtemisBankingPro.Core.Application.Features.Users.Commands.CreateCommerceUser;
using ArtemisBankingPro.Core.Application.Features.Users.Commands.CreateUser;
using ArtemisBankingPro.Core.Application.Features.Users.Commands.UpdateUser;
using ArtemisBankingPro.Core.Application.Features.Users.Queries.GetUserById;
using ArtemisBankingPro.Core.Domain.Constants;
using AutoMapper;

namespace ArtemisBankingPro.Core.Application.Mappings;

/// <summary>
/// Mapeos Commands/Queries de la API ↔ DTOs de los servicios de negocio.
/// La API siempre envía los tokens EN EL CUERPO del correo (TokenInBody = true).
/// </summary>
public class WebApiMappingProfile : Profile
{
    public WebApiMappingProfile()
    {
        // Usuarios
        CreateMap<CreateUserCommand, CreateUserRequest>()
            .ForMember(d => d.CommerceId, o => o.Ignore())
            .ForMember(d => d.TokenInBody, o => o.MapFrom(_ => true));

        CreateMap<CreateCommerceUserCommand, CreateUserRequest>()
            .ForMember(d => d.Role, o => o.MapFrom(_ => Roles.Comercio))
            .ForMember(d => d.TokenInBody, o => o.MapFrom(_ => true));

        CreateMap<UpdateUserCommand, UpdateUserRequest>();

        CreateMap<UserDto, UserWithMainAccountDto>()
            .ForMember(d => d.MainAccount, o => o.Ignore());

        // Préstamos
        CreateMap<CreateLoanCommand, CreateLoanRequest>();

        // Comercios
        CreateMap<CreateCommerceCommand, SaveCommerceRequest>();
        CreateMap<UpdateCommerceCommand, SaveCommerceRequest>();

        CreateMap<CommerceDto, CommerceApiDto>()
            .ForMember(d => d.HasAssociatedUser, o => o.Ignore());

        CreateMap<CommerceDto, CommerceDetailDto>()
            .ForMember(d => d.HasAssociatedUser, o => o.Ignore())
            .ForMember(d => d.AssociatedUser, o => o.Ignore());
    }
}
