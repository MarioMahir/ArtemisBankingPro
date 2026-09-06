using ArtemisBankingPro.Core.Application.Dtos.Commerces;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Application.Helpers;
using ArtemisBankingPro.Core.Domain.Entities;
using AutoMapper;

namespace ArtemisBankingPro.Core.Application.Mappings;

/// <summary>
/// Mapeos Entidad → DTO usados por los servicios de negocio.
/// Los campos de titular (ClientFullName/ClientIdentification) y los agregados
/// de préstamo se enriquecen aparte porque requieren consultas adicionales.
/// </summary>
public class EntityMappingProfile : Profile
{
    public EntityMappingProfile()
    {
        CreateMap<Commerce, CommerceDto>();

        CreateMap<SavingsAccount, SavingsAccountDto>()
            .ForMember(d => d.ClientFullName, o => o.Ignore())
            .ForMember(d => d.ClientIdentification, o => o.Ignore());

        CreateMap<LoanInstallment, LoanInstallmentDto>();

        // JAMÁS se expone el número completo: solo enmascarado y últimos 4.
        CreateMap<CreditCard, CreditCardDto>()
            .ForMember(d => d.MaskedCardNumber, o => o.MapFrom(s => CardFormatter.Mask(s.CardNumber)))
            .ForMember(d => d.LastFourDigits, o => o.MapFrom(s => CardFormatter.LastFour(s.CardNumber)))
            .ForMember(d => d.ExpirationDisplay, o => o.MapFrom(s => CardFormatter.ExpirationDisplay(s.ExpirationDate)))
            .ForMember(d => d.ClientFullName, o => o.Ignore())
            .ForMember(d => d.ClientIdentification, o => o.Ignore())
            .ForMember(d => d.Consumptions, o => o.Ignore());

        CreateMap<Loan, LoanDto>()
            .ForMember(d => d.MonthlyInstallment, o => o.MapFrom(s =>
                AmortizationCalculator.CalculateMonthlyInstallment(
                    s.ApprovedAmount, s.AnnualInterestRate, s.TermMonths)))
            .ForMember(d => d.ClientFullName, o => o.Ignore())
            .ForMember(d => d.ClientIdentification, o => o.Ignore())
            .ForMember(d => d.TotalInstallments, o => o.Ignore())
            .ForMember(d => d.PaidInstallments, o => o.Ignore())
            .ForMember(d => d.PendingAmount, o => o.Ignore())
            .ForMember(d => d.TotalAmountToPay, o => o.Ignore())
            .ForMember(d => d.IsInArrears, o => o.Ignore())
            .ForMember(d => d.Installments, o => o.Ignore());
    }
}
