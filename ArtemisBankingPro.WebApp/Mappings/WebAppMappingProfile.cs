using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.WebApp.ViewModels.Prestamos;
using ArtemisBankingPro.WebApp.ViewModels.Tarjetas;
using ArtemisBankingPro.WebApp.ViewModels.Usuarios;
using AutoMapper;

namespace ArtemisBankingPro.WebApp.Mappings;

/// <summary>Mapeos DTO ↔ ViewModel de la capa de presentación WebApp.</summary>
public class WebAppMappingProfile : Profile
{
    public WebAppMappingProfile()
    {
        // ---- Usuarios ----
        CreateMap<CrearUsuarioViewModel, CreateUserRequest>()
            .ForMember(d => d.CommerceId, o => o.Ignore())
            .ForMember(d => d.TokenInBody, o => o.MapFrom(_ => false));

        CreateMap<UserDto, EditarUsuarioViewModel>()
            .ForMember(d => d.Password, o => o.Ignore())
            .ForMember(d => d.ConfirmPassword, o => o.Ignore())
            .ForMember(d => d.AdditionalAmount, o => o.Ignore());

        CreateMap<EditarUsuarioViewModel, UpdateUserRequest>();

        // ---- Préstamos ----
        CreateMap<DatosPrestamoViewModel, CreateLoanRequest>();
        CreateMap<ConfirmarRiesgoViewModel, CreateLoanRequest>();

        CreateMap<UserDto, DatosPrestamoViewModel>()
            .ForMember(d => d.ClientId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.ClienteNombre, o => o.MapFrom(s => s.FullName))
            .ForMember(d => d.ClienteCedula, o => o.MapFrom(s => s.Identification))
            .ForMember(d => d.TermInMonths, o => o.Ignore())
            .ForMember(d => d.CapitalAmount, o => o.Ignore())
            .ForMember(d => d.AnnualInterestRate, o => o.Ignore());

        CreateMap<LoanDto, EditarTasaViewModel>()
            .ForMember(d => d.LoanId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.ClienteNombre, o => o.MapFrom(s => s.ClientFullName))
            .ForMember(d => d.TasaActual, o => o.MapFrom(s => s.AnnualInterestRate))
            .ForMember(d => d.NuevaTasa, o => o.Ignore());

        // ---- Tarjetas ----
        CreateMap<UserDto, AsignarTarjetaViewModel>()
            .ForMember(d => d.ClientId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.ClienteNombre, o => o.MapFrom(s => s.FullName))
            .ForMember(d => d.ClienteCedula, o => o.MapFrom(s => s.Identification))
            .ForMember(d => d.CreditLimit, o => o.Ignore());

        CreateMap<CreditCardDto, EditarLimiteViewModel>()
            .ForMember(d => d.CardId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.UltimosCuatro, o => o.MapFrom(s => s.LastFourDigits))
            .ForMember(d => d.ClienteNombre, o => o.MapFrom(s => s.ClientFullName))
            .ForMember(d => d.LimiteActual, o => o.MapFrom(s => s.CreditLimit))
            .ForMember(d => d.Deuda, o => o.MapFrom(s => s.OwedAmount))
            .ForMember(d => d.NuevoLimite, o => o.Ignore());
    }
}
