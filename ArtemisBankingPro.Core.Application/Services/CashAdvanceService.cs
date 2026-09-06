using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.CashAdvances;
using ArtemisBankingPro.Core.Application.Dtos.Email;
using ArtemisBankingPro.Core.Application.Helpers;
using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Application.Extensions;

namespace ArtemisBankingPro.Core.Application.Services;

public class CashAdvanceService(
    IGenericRepository<CreditCard> cardRepository,
    IGenericRepository<CardConsumption> consumptionRepository,
    IGenericRepository<SavingsAccount> accountRepository,
    IGenericRepository<Transaction> transactionRepository,
    IAccountService accountService,
    IEmailService emailService,
    IUnitOfWork unitOfWork) : ICashAdvanceService
{
    public async Task<ServiceResult<CashAdvanceConfirmationDto>> PrepareAsync(
        string ownerUserId, int cardId, string destinationAccountNumber, decimal advanceAmount)
    {
        var validation = await ValidateAsync(ownerUserId, cardId, destinationAccountNumber, advanceAmount);
        if (!validation.Succeeded)
            return ServiceResult<CashAdvanceConfirmationDto>.Fail(validation.Message!);

        var (card, account) = validation.Data!;
        return ServiceResult<CashAdvanceConfirmationDto>.Ok(new CashAdvanceConfirmationDto
        {
            CardLastFourDigits = CardFormatter.LastFour(card.CardNumber),
            DestinationAccountNumber = account.AccountNumber,
            AdvanceAmount = advanceAmount,
            InterestAmount = CashAdvanceCalculator.Interest(advanceAmount),
            TotalToCharge = CashAdvanceCalculator.TotalToCharge(advanceAmount),
            AvailableCredit = CashAdvanceCalculator.AvailableCredit(card.CreditLimit, card.OwedAmount)
        });
    }

    public async Task<ServiceResult> ExecuteAsync(
        string ownerUserId, int cardId, string destinationAccountNumber, decimal advanceAmount)
    {
        var validation = await ValidateAsync(ownerUserId, cardId, destinationAccountNumber, advanceAmount);
        if (!validation.Succeeded)
            return ServiceResult.Fail(validation.Message!);

        var (card, account) = validation.Data!;
        var totalToCharge = CashAdvanceCalculator.TotalToCharge(advanceAmount);
        var lastFour = CardFormatter.LastFour(card.CardNumber);

        // Rechazo: consumo RECHAZADO por el total, sin tocar balances ni deudas.
        if (!CashAdvanceCalculator.IsApproved(advanceAmount, card.CreditLimit, card.OwedAmount))
        {
            await consumptionRepository.AddAsync(new CardConsumption
            {
                CreditCardId = card.Id,
                CommerceName = AppConstants.CashAdvanceCommerceName,
                Amount = totalToCharge,
                Status = ConsumptionStatus.Rechazado,
                CreatedAt = DateTime.Now
            });
            await unitOfWork.SaveChangesAsync();
            return ServiceResult.Fail(Mensajes.AvanceExcedeDisponible);
        }

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // La tarjeta carga avance + interés; la cuenta recibe SOLO el avance.
            card.OwedAmount += totalToCharge;
            cardRepository.Update(card);

            await consumptionRepository.AddAsync(new CardConsumption
            {
                CreditCardId = card.Id,
                CommerceName = AppConstants.CashAdvanceCommerceName,
                Amount = totalToCharge,
                Status = ConsumptionStatus.Aprobado,
                CreatedAt = DateTime.Now
            });

            account.Balance += advanceAmount;
            accountRepository.Update(account);

            await transactionRepository.AddAsync(new Transaction
            {
                AccountId = account.Id,
                Amount = advanceAmount,
                Type = TransactionType.Credito,
                Beneficiary = account.AccountNumber,
                Origin = lastFour,
                Status = TransactionStatus.Aprobada,
                CreatedAt = DateTime.Now
            });
        });

        var owner = await accountService.GetByIdAsync(ownerUserId);
        var sent = owner is null || await emailService.SendAsync(new EmailRequest
        {
            To = owner.Email,
            Subject = $"Avance de efectivo desde la tarjeta {lastFour}",
            HtmlBody = $"""
                <h2>Avance de efectivo desde la tarjeta {lastFour}</h2>
                <p>Avance de <strong>RD${advanceAmount:#,##0.00}</strong> acreditado a su cuenta
                <strong>{account.AccountNumber}</strong>.</p>
                <p>Interés (6.25%): <strong>RD${CashAdvanceCalculator.Interest(advanceAmount):#,##0.00}</strong>.
                Total cargado a la tarjeta: <strong>RD${totalToCharge:#,##0.00}</strong>.</p>
                """
        });

        return sent ? ServiceResult.Ok() : ServiceResult.Ok(Mensajes.TransaccionCorreoFallido);
    }

    private async Task<ServiceResult<(CreditCard Card, SavingsAccount Account)>> ValidateAsync(
        string ownerUserId, int cardId, string destinationAccountNumber, decimal advanceAmount)
    {
        var card = await cardRepository.Query()
            .FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == ownerUserId);

        if (card is null)
            return ServiceResult<(CreditCard, SavingsAccount)>.Fail(Mensajes.TarjetaNoExiste);

        if (card.Status != ProductStatus.Activa)
            return ServiceResult<(CreditCard, SavingsAccount)>.Fail(Mensajes.TarjetaNoActiva);

        if (card.ExpirationDate < DateTime.Now)
            return ServiceResult<(CreditCard, SavingsAccount)>.Fail(Mensajes.TarjetaVencida);

        var account = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.AccountNumber == destinationAccountNumber && a.UserId == ownerUserId);

        if (account is null || account.Status != ProductStatus.Activa)
            return ServiceResult<(CreditCard, SavingsAccount)>.Fail(Mensajes.CuentaAhorroNoActiva);

        if (advanceAmount <= 0)
            return ServiceResult<(CreditCard, SavingsAccount)>.Fail(Mensajes.MontoAvanceInvalido);

        return ServiceResult<(CreditCard, SavingsAccount)>.Ok((card, account));
    }
}
