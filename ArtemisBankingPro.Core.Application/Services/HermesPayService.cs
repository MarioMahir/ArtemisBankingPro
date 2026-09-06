using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Dtos.Email;
using ArtemisBankingPro.Core.Application.Helpers;
using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Application.Extensions;

namespace ArtemisBankingPro.Core.Application.Services;

public class HermesPayService(
    IGenericRepository<CreditCard> cardRepository,
    IGenericRepository<CardConsumption> consumptionRepository,
    IGenericRepository<Commerce> commerceRepository,
    IGenericRepository<SavingsAccount> accountRepository,
    IGenericRepository<Transaction> transactionRepository,
    IHashingService hashingService,
    IAccountService accountService,
    IEmailService emailService,
    IUnitOfWork unitOfWork) : IHermesPayService
{
    public async Task<ServiceResult<PagedResult<ConsumptionDto>>> GetTransactionsAsync(int commerceId, int page)
    {
        var commerce = await commerceRepository.GetByIdAsync(commerceId);
        if (commerce is null)
            return ServiceResult<PagedResult<ConsumptionDto>>.Fail(Mensajes.ComercioNoExiste);

        var (p, size) = PagedResult<ConsumptionDto>.Normalize(page, null);
        var query = consumptionRepository.Query().Where(c => c.CommerceId == commerceId);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((p - 1) * size)
            .Take(size)
            .Select(c => new
            {
                c.Id,
                c.CommerceName,
                c.Amount,
                c.Status,
                c.CreatedAt,
                c.CreditCard.CardNumber
            })
            .ToListAsync();

        return ServiceResult<PagedResult<ConsumptionDto>>.Ok(new PagedResult<ConsumptionDto>
        {
            Items = rows.Select(r => new ConsumptionDto
            {
                Id = r.Id,
                CardLastFourDigits = CardFormatter.LastFour(r.CardNumber),
                CommerceName = r.CommerceName,
                Amount = r.Amount,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            }).ToList(),
            Page = p,
            PageSize = size,
            TotalCount = total
        });
    }

    public async Task<ServiceResult> ProcessPaymentAsync(
        int commerceId, string cardNumber, string monthExpirationCard, string yearExpirationCard,
        string cvc, decimal transactionAmount)
    {
        // 1) Tarjeta: existencia, estado, vigencia (comparación MM-AAAA) y CVC.
        var card = await cardRepository.Query()
            .FirstOrDefaultAsync(c => c.CardNumber == cardNumber);
        if (card is null)
            return ServiceResult.Fail(Mensajes.TarjetaInvalidaCajero);

        if (card.Status != ProductStatus.Activa)
            return ServiceResult.Fail(Mensajes.TarjetaNoActiva);

        if (!int.TryParse(monthExpirationCard, out var month)
            || !int.TryParse(yearExpirationCard, out var year)
            || card.ExpirationDate.Month != month
            || card.ExpirationDate.Year != year)
            return ServiceResult.Fail(Mensajes.TarjetaInvalidaCajero);

        var now = DateTime.Now;
        if (year < now.Year || (year == now.Year && month < now.Month))
            return ServiceResult.Fail(Mensajes.TarjetaVencida);

        if (!string.Equals(hashingService.Sha256(cvc), card.CvcHash, StringComparison.OrdinalIgnoreCase))
            return ServiceResult.Fail(Mensajes.TarjetaInvalidaCajero);

        // 2) Comercio: existe, activo, con usuario asociado y cuenta principal activa.
        var commerce = await commerceRepository.GetByIdAsync(commerceId);
        if (commerce is null)
            return ServiceResult.Fail(Mensajes.ComercioNoExiste);

        if (!commerce.IsActive)
            return ServiceResult.Fail(Mensajes.ComercioInactivo);

        var commerceUser = await accountService.GetByCommerceIdAsync(commerceId);
        if (commerceUser is null)
            return ServiceResult.Fail(Mensajes.ComercioSinUsuario);

        var commerceAccount = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.UserId == commerceUser.Id
                && a.Type == AccountType.Principal
                && a.Status == ProductStatus.Activa);
        if (commerceAccount is null)
            return ServiceResult.Fail(Mensajes.ComercioSinCuentaPrincipal);

        // 3) Crédito disponible = límite − deuda. Rechazo → consumo RECHAZADO sin tocar nada.
        if (transactionAmount > card.CreditLimit - card.OwedAmount)
        {
            await consumptionRepository.AddAsync(new CardConsumption
            {
                CreditCardId = card.Id,
                CommerceId = commerce.Id,
                CommerceName = commerce.Name,
                Amount = transactionAmount,
                Status = ConsumptionStatus.Rechazado,
                CreatedAt = DateTime.Now
            });
            await unitOfWork.SaveChangesAsync();
            return ServiceResult.Fail(Mensajes.ApiExcedeCreditoDisponible);
        }

        var lastFour = CardFormatter.LastFour(card.CardNumber);

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            card.OwedAmount += transactionAmount;
            cardRepository.Update(card);

            await consumptionRepository.AddAsync(new CardConsumption
            {
                CreditCardId = card.Id,
                CommerceId = commerce.Id,
                CommerceName = commerce.Name,
                Amount = transactionAmount,
                Status = ConsumptionStatus.Aprobado,
                CreatedAt = DateTime.Now
            });

            commerceAccount.Balance += transactionAmount;
            accountRepository.Update(commerceAccount);

            await transactionRepository.AddAsync(new Transaction
            {
                AccountId = commerceAccount.Id,
                Amount = transactionAmount,
                Type = TransactionType.Credito,
                Beneficiary = commerceAccount.AccountNumber,
                Origin = lastFour,
                Status = TransactionStatus.Aprobada,
                CreatedAt = DateTime.Now
            });
        });

        // Correos: al cliente dueño de la tarjeta y al comercio (fallo no revierte).
        var cardOwner = await accountService.GetByIdAsync(card.UserId);
        if (cardOwner is not null)
        {
            await emailService.SendAsync(new EmailRequest
            {
                To = cardOwner.Email,
                Subject = $"Consumo realizado con la tarjeta {lastFour}",
                HtmlBody = $"""
                    <h2>Consumo realizado con la tarjeta {lastFour}</h2>
                    <p>Consumo de <strong>RD${transactionAmount:#,##0.00}</strong> en
                    <strong>{commerce.Name}</strong> con su tarjeta terminada en <strong>{lastFour}</strong>.</p>
                    """
            });
        }

        await emailService.SendAsync(new EmailRequest
        {
            To = commerceUser.Email,
            Subject = $"Pago recibido a través de tarjeta {lastFour}",
            HtmlBody = $"""
                <h2>Pago recibido a través de tarjeta {lastFour}</h2>
                <p>Su comercio <strong>{commerce.Name}</strong> recibió un pago de
                <strong>RD${transactionAmount:#,##0.00}</strong> con la tarjeta terminada en
                <strong>{lastFour}</strong>, acreditado a la cuenta <strong>{commerceAccount.AccountNumber}</strong>.</p>
                """
        });

        return ServiceResult.Ok();
    }
}
