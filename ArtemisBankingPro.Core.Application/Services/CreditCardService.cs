using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Dtos.Email;
using ArtemisBankingPro.Core.Application.Helpers;
using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using AutoMapper;
using ArtemisBankingPro.Core.Application.Extensions;

namespace ArtemisBankingPro.Core.Application.Services;

public class CreditCardService(
    IGenericRepository<CreditCard> cardRepository,
    IGenericRepository<CardConsumption> consumptionRepository,
    IProductNumberGenerator numberGenerator,
    IHashingService hashingService,
    IAccountService accountService,
    IEmailService emailService,
    IUnitOfWork unitOfWork,
    IMapper mapper) : ICreditCardService
{
    public async Task<ServiceResult<PagedResult<CreditCardDto>>> GetPagedAsync(
        int page, ProductStatus? status, string? identification)
    {
        var (p, size) = PagedResult<CreditCardDto>.Normalize(page, null);
        var query = cardRepository.Query();

        if (!string.IsNullOrWhiteSpace(identification))
        {
            var client = await accountService.GetByIdentificationAsync(identification.Trim());
            if (client is null)
                return ServiceResult<PagedResult<CreditCardDto>>.Fail(Mensajes.ClienteNoExistePorCedula);

            query = query.Where(c => c.UserId == client.Id);
        }

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var total = await query.CountAsync();

        var ordered = status.HasValue
            ? query.OrderByDescending(c => c.CreatedAt)
            : query.OrderBy(c => c.Status).ThenByDescending(c => c.CreatedAt);

        var items = await ordered.Skip((p - 1) * size).Take(size).ToListAsync();
        var dtos = items.Select(mapper.Map<CreditCardDto>).ToList();
        await EnrichWithClientsAsync(dtos);

        return ServiceResult<PagedResult<CreditCardDto>>.Ok(new PagedResult<CreditCardDto>
        {
            Items = dtos,
            Page = p,
            PageSize = size,
            TotalCount = total
        });
    }

    public async Task<ServiceResult<CreditCardDto>> AssignAsync(string clientId, decimal creditLimit, string adminUserId)
    {
        var client = await accountService.GetByIdAsync(clientId);
        if (client is null || !client.IsActive || client.Role != Roles.Cliente)
            return ServiceResult<CreditCardDto>.Fail(Mensajes.SoloTarjetasClientesActivos);

        if (creditLimit <= 0)
            return ServiceResult<CreditCardDto>.Fail(Mensajes.LimiteInvalido);

        var cardNumber = await numberGenerator.GenerateCardNumberAsync();
        var cvc = numberGenerator.GenerateCvc();
        var createdAt = DateTime.Now;

        var card = new CreditCard
        {
            CardNumber = cardNumber,
            UserId = clientId,
            AdminUserId = adminUserId,
            CreditLimit = creditLimit,
            OwedAmount = 0m, // la deuda inicial siempre es RD$0.00
            ExpirationDate = createdAt.AddYears(AppConstants.CardExpirationYears),
            CvcHash = hashingService.Sha256(cvc),
            Status = ProductStatus.Activa,
            CreatedAt = createdAt
        };

        await cardRepository.AddAsync(card);
        await unitOfWork.SaveChangesAsync();

        // El correo JAMÁS incluye el CVC ni el número completo.
        await emailService.SendAsync(new EmailRequest
        {
            To = client.Email,
            Subject = "Tarjeta de crédito asignada",
            HtmlBody = $"""
                <h2>Tarjeta de crédito asignada</h2>
                <p>Se le ha asignado una tarjeta de crédito con los siguientes datos:</p>
                <ul>
                    <li>Tarjeta terminada en: <strong>{CardFormatter.LastFour(cardNumber)}</strong></li>
                    <li>Límite de crédito: <strong>RD${creditLimit:#,##0.00}</strong></li>
                    <li>Expiración: <strong>{CardFormatter.ExpirationDisplay(card.ExpirationDate)}</strong></li>
                    <li>Fecha de asignación: <strong>{createdAt:dd/MM/yyyy}</strong></li>
                </ul>
                """
        });

        return ServiceResult<CreditCardDto>.Ok(mapper.Map<CreditCardDto>(card));
    }

    public async Task<ServiceResult<CreditCardDto>> GetDetailAsync(int cardId)
    {
        var card = await cardRepository.Query().FirstOrDefaultAsync(c => c.Id == cardId);
        if (card is null)
            return ServiceResult<CreditCardDto>.Fail(Mensajes.TarjetaNoExiste);

        var consumptions = await consumptionRepository.Query()
            .Where(c => c.CreditCardId == cardId)
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .ToListAsync();

        var dto = mapper.Map<CreditCardDto>(card);
        dto.Consumptions = consumptions.Select(c => new ConsumptionDto
        {
            Id = c.Id,
            CardLastFourDigits = dto.LastFourDigits,
            CommerceName = c.CommerceName,
            Amount = c.Amount,
            Status = c.Status,
            CreatedAt = c.CreatedAt
        }).ToList();

        await EnrichWithClientsAsync([dto]);
        return ServiceResult<CreditCardDto>.Ok(dto);
    }

    public async Task<ServiceResult> UpdateLimitAsync(int cardId, decimal newLimit)
    {
        var card = await cardRepository.Query().FirstOrDefaultAsync(c => c.Id == cardId);
        if (card is null)
            return ServiceResult.Fail(Mensajes.TarjetaNoExiste);

        if (card.Status == ProductStatus.Cancelada)
            return ServiceResult.Fail(Mensajes.TarjetaCanceladaNoModificable);

        if (newLimit <= 0)
            return ServiceResult.Fail(Mensajes.LimiteTarjetaInvalido);

        if (newLimit < card.OwedAmount)
            return ServiceResult.Fail(Mensajes.LimiteMenorQueDeuda);

        card.CreditLimit = newLimit;
        cardRepository.Update(card);
        await unitOfWork.SaveChangesAsync();

        var client = await accountService.GetByIdAsync(card.UserId);
        if (client is not null)
        {
            await emailService.SendAsync(new EmailRequest
            {
                To = client.Email,
                Subject = "Límite de tarjeta actualizado",
                HtmlBody = $"""
                    <h2>Límite de tarjeta actualizado</h2>
                    <p>El límite de su tarjeta terminada en <strong>{CardFormatter.LastFour(card.CardNumber)}</strong>
                    ha sido actualizado a <strong>RD${newLimit:#,##0.00}</strong>.</p>
                    """
            });
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CancelAsync(int cardId)
    {
        var card = await cardRepository.Query().FirstOrDefaultAsync(c => c.Id == cardId);
        if (card is null)
            return ServiceResult.Fail(Mensajes.TarjetaNoExiste);

        if (card.Status == ProductStatus.Cancelada)
            return ServiceResult.Fail(Mensajes.TarjetaCanceladaNoModificable);

        if (card.OwedAmount > 0)
            return ServiceResult.Fail(Mensajes.TarjetaConDeudaNoCancelable);

        card.Status = ProductStatus.Cancelada;
        cardRepository.Update(card);
        await unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<List<CreditCardDto>> GetActiveCardsByUserAsync(string userId)
    {
        var cards = await cardRepository.Query()
            .Where(c => c.UserId == userId && c.Status == ProductStatus.Activa)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return cards.Select(mapper.Map<CreditCardDto>).ToList();
    }

    public async Task<string?> GetCardNumberForOwnerAsync(int cardId, string ownerUserId)
    {
        return await cardRepository.Query()
            .Where(c => c.Id == cardId && c.UserId == ownerUserId)
            .Select(c => c.CardNumber)
            .FirstOrDefaultAsync();
    }

    // ---------------- Helpers ----------------

    private async Task EnrichWithClientsAsync(List<CreditCardDto> dtos)
    {
        if (dtos.Count == 0) return;

        var users = await accountService.GetByIdsAsync(dtos.Select(d => d.UserId));
        foreach (var dto in dtos)
        {
            if (users.TryGetValue(dto.UserId, out var user))
            {
                dto.ClientFullName = user.FullName;
                dto.ClientIdentification = user.Identification;
            }
        }
    }

}
