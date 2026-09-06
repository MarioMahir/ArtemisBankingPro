using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Email;
using ArtemisBankingPro.Core.Application.Dtos.Transactions;
using ArtemisBankingPro.Core.Application.Helpers;
using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Application.Extensions;

namespace ArtemisBankingPro.Core.Application.Services;

public class TransactionService(
    IGenericRepository<SavingsAccount> accountRepository,
    IGenericRepository<Transaction> transactionRepository,
    IGenericRepository<CreditCard> cardRepository,
    IGenericRepository<Loan> loanRepository,
    IGenericRepository<LoanInstallment> installmentRepository,
    IGenericRepository<Beneficiary> beneficiaryRepository,
    IAccountService accountService,
    IEmailService emailService,
    IUnitOfWork unitOfWork) : ITransactionService
{
    // =============================================================
    // Transacción express (cliente) / pago a terceros (cajero)
    // =============================================================

    public async Task<ServiceResult<TransactionConfirmationDto>> PrepareExpressTransferAsync(
        string sourceAccountNumber, string destinationAccountNumber, decimal amount,
        string? ownerUserId = null, string? cashierId = null)
    {
        var validation = await ValidateTransferAsync(
            sourceAccountNumber, destinationAccountNumber, amount, ownerUserId, cashierId is not null);
        if (!validation.Succeeded)
            return ServiceResult<TransactionConfirmationDto>.Fail(validation.Message!);

        var (source, destination) = validation.Data!;
        return ServiceResult<TransactionConfirmationDto>.Ok(
            await BuildTransferConfirmationAsync(source, destination, amount));
    }

    public async Task<ServiceResult> ExpressTransferAsync(
        string sourceAccountNumber, string destinationAccountNumber, decimal amount,
        string? ownerUserId = null, string? cashierId = null)
    {
        var validation = await ValidateTransferAsync(
            sourceAccountNumber, destinationAccountNumber, amount, ownerUserId, cashierId is not null);
        if (!validation.Succeeded)
            return ServiceResult.Fail(validation.Message!);

        var (source, destination) = validation.Data!;
        return await ExecuteTransferAsync(source, destination, amount, cashierId,
            Mensajes.FondosInsuficientes, sendCrossEmails: true);
    }

    // =============================================================
    // Pago a tarjeta de crédito
    // =============================================================

    public async Task<ServiceResult<TransactionConfirmationDto>> PrepareCreditCardPaymentAsync(
        string sourceAccountNumber, string cardNumber, decimal amount,
        string? ownerUserId = null, string? cashierId = null)
    {
        var validation = await ValidateCardPaymentAsync(sourceAccountNumber, cardNumber, amount, ownerUserId, cashierId is not null);
        if (!validation.Succeeded)
            return ServiceResult<TransactionConfirmationDto>.Fail(validation.Message!);

        var (source, card) = validation.Data!;
        var cardOwner = await accountService.GetByIdAsync(card.UserId);

        return ServiceResult<TransactionConfirmationDto>.Ok(new TransactionConfirmationDto
        {
            SourceAccountNumber = source.AccountNumber,
            SourceHolderFullName = (await accountService.GetByIdAsync(source.UserId))?.FullName,
            DestinationHolderFullName = cardOwner?.FullName,
            ProductReference = CardFormatter.LastFour(card.CardNumber),
            RequestedAmount = amount,
            EffectiveAmount = PaymentDistributor.EffectiveAmount(amount, card.OwedAmount)
        });
    }

    public async Task<ServiceResult> PayCreditCardAsync(
        string sourceAccountNumber, string cardNumber, decimal amount,
        string? ownerUserId = null, string? cashierId = null)
    {
        var validation = await ValidateCardPaymentAsync(sourceAccountNumber, cardNumber, amount, ownerUserId, cashierId is not null);
        if (!validation.Succeeded)
            return ServiceResult.Fail(validation.Message!);

        var (source, card) = validation.Data!;
        var lastFour = CardFormatter.LastFour(card.CardNumber);

        // Anti-sobrepago: el excedente NO se debita.
        var effective = PaymentDistributor.EffectiveAmount(amount, card.OwedAmount);

        if (source.Balance < effective)
        {
            await RegisterRejectedDebitAsync(source, effective, lastFour, source.AccountNumber, cashierId, isPayment: true);
            return ServiceResult.Fail(Mensajes.SinFondosParaTransaccion);
        }

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            source.Balance -= effective;
            card.OwedAmount -= effective;
            accountRepository.Update(source);
            cardRepository.Update(card);

            await transactionRepository.AddAsync(new Transaction
            {
                AccountId = source.Id,
                Amount = effective,
                Type = TransactionType.Debito,
                Beneficiary = lastFour,
                Origin = source.AccountNumber,
                Status = TransactionStatus.Aprobada,
                CashierId = cashierId,
                IsPayment = true,
                CreatedAt = DateTime.Now
            });
        });

        // Correo al dueño de la tarjeta y también al dueño de la cuenta si es distinto.
        var subject = $"Pago realizado a la tarjeta {lastFour}";
        var body = $"""
            <h2>{subject}</h2>
            <p>Se realizó un pago de <strong>RD${effective:#,##0.00}</strong> a la tarjeta terminada en
            <strong>{lastFour}</strong> desde la cuenta <strong>{source.AccountNumber}</strong>.</p>
            """;

        var allSent = await SendToOwnersAsync(card.UserId, source.UserId, subject, body);
        return allSent ? ServiceResult.Ok() : ServiceResult.Ok(Mensajes.TransaccionCorreoFallido);
    }

    // =============================================================
    // Pago a préstamo
    // =============================================================

    public async Task<ServiceResult<TransactionConfirmationDto>> PrepareLoanPaymentAsync(
        string sourceAccountNumber, string loanNumber, decimal amount,
        string? ownerUserId = null, string? cashierId = null)
    {
        var validation = await ValidateLoanPaymentAsync(sourceAccountNumber, loanNumber, amount, ownerUserId, cashierId is not null);
        if (!validation.Succeeded)
            return ServiceResult<TransactionConfirmationDto>.Fail(validation.Message!);

        var (source, loan, totalPending) = validation.Data!;
        return ServiceResult<TransactionConfirmationDto>.Ok(new TransactionConfirmationDto
        {
            SourceAccountNumber = source.AccountNumber,
            SourceHolderFullName = (await accountService.GetByIdAsync(source.UserId))?.FullName,
            DestinationHolderFullName = (await accountService.GetByIdAsync(loan.UserId))?.FullName,
            ProductReference = loan.LoanNumber,
            RequestedAmount = amount,
            EffectiveAmount = PaymentDistributor.EffectiveAmount(amount, totalPending)
        });
    }

    public async Task<ServiceResult> PayLoanAsync(
        string sourceAccountNumber, string loanNumber, decimal amount,
        string? ownerUserId = null, string? cashierId = null)
    {
        var validation = await ValidateLoanPaymentAsync(sourceAccountNumber, loanNumber, amount, ownerUserId, cashierId is not null);
        if (!validation.Succeeded)
            return ServiceResult.Fail(validation.Message!);

        var (source, loan, totalPending) = validation.Data!;
        var effective = PaymentDistributor.EffectiveAmount(amount, totalPending);

        if (source.Balance < effective)
        {
            await RegisterRejectedDebitAsync(source, effective, loan.LoanNumber, source.AccountNumber, cashierId, isPayment: true);
            return ServiceResult.Fail(Mensajes.SinFondosParaTransaccion);
        }

        var installments = await installmentRepository.Query()
            .Where(i => i.LoanId == loan.Id)
            .ToListAsync();

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            source.Balance -= effective;
            accountRepository.Update(source);

            // Cascada cuota a cuota desde la más antigua (marca Pagada/Parcial y quita atraso).
            var allPaid = PaymentDistributor.ApplyPayment(installments, effective);
            foreach (var installment in installments)
                installmentRepository.Update(installment);

            if (allPaid)
            {
                loan.Status = LoanStatus.Completado;
                loanRepository.Update(loan);
            }

            await transactionRepository.AddAsync(new Transaction
            {
                AccountId = source.Id,
                Amount = effective,
                Type = TransactionType.Debito,
                Beneficiary = loan.LoanNumber,
                Origin = source.AccountNumber,
                Status = TransactionStatus.Aprobada,
                CashierId = cashierId,
                IsPayment = true,
                CreatedAt = DateTime.Now
            });
        });

        var subject = $"Pago realizado al préstamo {loan.LoanNumber}";
        var body = $"""
            <h2>{subject}</h2>
            <p>Se realizó un pago de <strong>RD${effective:#,##0.00}</strong> al préstamo
            <strong>{loan.LoanNumber}</strong> desde la cuenta <strong>{source.AccountNumber}</strong>.</p>
            """;

        var allSent = await SendToOwnersAsync(loan.UserId, source.UserId, subject, body);
        return allSent ? ServiceResult.Ok() : ServiceResult.Ok(Mensajes.TransaccionCorreoFallido);
    }

    // =============================================================
    // Transferencia entre cuentas propias
    // =============================================================

    public async Task<ServiceResult<TransactionConfirmationDto>> PrepareTransferBetweenOwnAccountsAsync(
        string ownerUserId, string sourceAccountNumber, string destinationAccountNumber, decimal amount)
    {
        var validation = await ValidateOwnTransferAsync(ownerUserId, sourceAccountNumber, destinationAccountNumber, amount);
        if (!validation.Succeeded)
            return ServiceResult<TransactionConfirmationDto>.Fail(validation.Message!);

        var (source, destination) = validation.Data!;
        return ServiceResult<TransactionConfirmationDto>.Ok(
            await BuildTransferConfirmationAsync(source, destination, amount));
    }

    public async Task<ServiceResult> TransferBetweenOwnAccountsAsync(
        string ownerUserId, string sourceAccountNumber, string destinationAccountNumber, decimal amount)
    {
        var validation = await ValidateOwnTransferAsync(ownerUserId, sourceAccountNumber, destinationAccountNumber, amount);
        if (!validation.Succeeded)
            return ServiceResult.Fail(validation.Message!);

        var (source, destination) = validation.Data!;

        if (source.Balance < amount)
        {
            await RegisterRejectedDebitAsync(source, amount, destination.AccountNumber, source.AccountNumber, null, isPayment: false);
            return ServiceResult.Fail(Mensajes.SinMontoRequerido);
        }

        await ExecuteApprovedTransferAsync(source, destination, amount, null);

        // Una única notificación (ambas cuentas son del mismo titular).
        var owner = await accountService.GetByIdAsync(ownerUserId);
        var sent = owner is null || await emailService.SendAsync(new EmailRequest
        {
            To = owner.Email,
            Subject = "Transferencia entre cuentas realizada",
            HtmlBody = $"""
                <h2>Transferencia entre cuentas realizada</h2>
                <p>Transferencia de <strong>RD${amount:#,##0.00}</strong> desde su cuenta
                <strong>{source.AccountNumber}</strong> hacia su cuenta <strong>{destination.AccountNumber}</strong>.</p>
                """
        });

        return sent ? ServiceResult.Ok() : ServiceResult.Ok(Mensajes.TransaccionCorreoFallido);
    }

    // =============================================================
    // Transferencia a beneficiario
    // =============================================================

    public async Task<ServiceResult<TransactionConfirmationDto>> PrepareTransferToBeneficiaryAsync(
        string ownerUserId, int beneficiaryId, string sourceAccountNumber, decimal amount)
    {
        var validation = await ValidateBeneficiaryTransferAsync(ownerUserId, beneficiaryId, sourceAccountNumber, amount);
        if (!validation.Succeeded)
            return ServiceResult<TransactionConfirmationDto>.Fail(validation.Message!);

        var (source, destination) = validation.Data!;
        return ServiceResult<TransactionConfirmationDto>.Ok(
            await BuildTransferConfirmationAsync(source, destination, amount));
    }

    public async Task<ServiceResult> TransferToBeneficiaryAsync(
        string ownerUserId, int beneficiaryId, string sourceAccountNumber, decimal amount)
    {
        var validation = await ValidateBeneficiaryTransferAsync(ownerUserId, beneficiaryId, sourceAccountNumber, amount);
        if (!validation.Succeeded)
            return ServiceResult.Fail(validation.Message!);

        var (source, destination) = validation.Data!;
        return await ExecuteTransferAsync(source, destination, amount, null,
            Mensajes.SinFondosParaTransaccion, sendCrossEmails: true);
    }

    // =============================================================
    // Cajero: depósito y retiro
    // =============================================================

    public async Task<ServiceResult<TransactionConfirmationDto>> PrepareDepositAsync(string accountNumber, decimal amount)
    {
        if (amount <= 0)
            return ServiceResult<TransactionConfirmationDto>.Fail(Mensajes.MontoDepositoInvalido);

        var account = await GetActiveAccountAsync(accountNumber);
        if (account is null)
            return ServiceResult<TransactionConfirmationDto>.Fail(Mensajes.CuentaInvalida);

        var holder = await accountService.GetByIdAsync(account.UserId);
        return ServiceResult<TransactionConfirmationDto>.Ok(new TransactionConfirmationDto
        {
            DestinationAccountNumber = account.AccountNumber,
            DestinationHolderFullName = holder?.FullName,
            RequestedAmount = amount,
            EffectiveAmount = amount
        });
    }

    public async Task<ServiceResult> DepositAsync(string accountNumber, decimal amount, string cashierId)
    {
        if (amount <= 0)
            return ServiceResult.Fail(Mensajes.MontoDepositoInvalido);

        var account = await GetActiveAccountAsync(accountNumber);
        if (account is null)
            return ServiceResult.Fail(Mensajes.CuentaInvalida);

        account.Balance += amount;
        accountRepository.Update(account);

        await transactionRepository.AddAsync(new Transaction
        {
            AccountId = account.Id,
            Amount = amount,
            Type = TransactionType.Credito,
            Beneficiary = account.AccountNumber,
            Origin = AppConstants.DepositOrigin,
            Status = TransactionStatus.Aprobada,
            CashierId = cashierId,
            CreatedAt = DateTime.Now
        });
        await unitOfWork.SaveChangesAsync();

        var holder = await accountService.GetByIdAsync(account.UserId);
        var lastFour = LastFour(account.AccountNumber);
        var sent = holder is null || await emailService.SendAsync(new EmailRequest
        {
            To = holder.Email,
            Subject = $"Depósito realizado a su cuenta {lastFour}",
            HtmlBody = $"""
                <h2>Depósito realizado a su cuenta {lastFour}</h2>
                <p>Se depositaron <strong>RD${amount:#,##0.00}</strong> en su cuenta
                <strong>{account.AccountNumber}</strong>.</p>
                """
        });

        return sent ? ServiceResult.Ok() : ServiceResult.Ok(Mensajes.DepositoCorreoFallido);
    }

    public async Task<ServiceResult<TransactionConfirmationDto>> PrepareWithdrawAsync(string accountNumber, decimal amount)
    {
        if (amount <= 0)
            return ServiceResult<TransactionConfirmationDto>.Fail(Mensajes.MontoRetiroInvalido);

        var account = await GetActiveAccountAsync(accountNumber);
        if (account is null)
            return ServiceResult<TransactionConfirmationDto>.Fail(Mensajes.CuentaInvalida);

        var holder = await accountService.GetByIdAsync(account.UserId);
        return ServiceResult<TransactionConfirmationDto>.Ok(new TransactionConfirmationDto
        {
            SourceAccountNumber = account.AccountNumber,
            SourceHolderFullName = holder?.FullName,
            RequestedAmount = amount,
            EffectiveAmount = amount
        });
    }

    public async Task<ServiceResult> WithdrawAsync(string accountNumber, decimal amount, string cashierId)
    {
        if (amount <= 0)
            return ServiceResult.Fail(Mensajes.MontoRetiroInvalido);

        var account = await GetActiveAccountAsync(accountNumber);
        if (account is null)
            return ServiceResult.Fail(Mensajes.CuentaInvalida);

        if (account.Balance < amount)
        {
            await RegisterRejectedDebitAsync(account, amount, AppConstants.WithdrawalBeneficiary,
                account.AccountNumber, cashierId, isPayment: false);
            return ServiceResult.Fail(Mensajes.RetiroExcedeSaldo);
        }

        account.Balance -= amount;
        accountRepository.Update(account);

        await transactionRepository.AddAsync(new Transaction
        {
            AccountId = account.Id,
            Amount = amount,
            Type = TransactionType.Debito,
            Beneficiary = AppConstants.WithdrawalBeneficiary,
            Origin = account.AccountNumber,
            Status = TransactionStatus.Aprobada,
            CashierId = cashierId,
            CreatedAt = DateTime.Now
        });
        await unitOfWork.SaveChangesAsync();

        var holder = await accountService.GetByIdAsync(account.UserId);
        var lastFour = LastFour(account.AccountNumber);
        var sent = holder is null || await emailService.SendAsync(new EmailRequest
        {
            To = holder.Email,
            Subject = $"Retiro realizado desde su cuenta {lastFour}",
            HtmlBody = $"""
                <h2>Retiro realizado desde su cuenta {lastFour}</h2>
                <p>Se retiraron <strong>RD${amount:#,##0.00}</strong> de su cuenta
                <strong>{account.AccountNumber}</strong>.</p>
                """
        });

        return sent ? ServiceResult.Ok() : ServiceResult.Ok(Mensajes.TransaccionCorreoFallido);
    }

    // =============================================================
    // Validaciones compartidas
    // =============================================================

    private async Task<ServiceResult<(SavingsAccount Source, SavingsAccount Destination)>> ValidateTransferAsync(
        string sourceNumber, string destinationNumber, decimal amount, string? ownerUserId, bool isCashier)
    {
        var source = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.AccountNumber == sourceNumber);

        if (source is null || (ownerUserId is not null && source.UserId != ownerUserId))
            return Fail<(SavingsAccount, SavingsAccount)>(isCashier ? Mensajes.CuentaOrigenInvalida : Mensajes.CuentaInvalida);

        if (source.Status != ProductStatus.Activa)
            return Fail<(SavingsAccount, SavingsAccount)>(isCashier ? Mensajes.CuentaOrigenInvalida : Mensajes.CuentaAhorroNoActiva);

        var destination = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.AccountNumber == destinationNumber);

        if (destination is null || destination.Status != ProductStatus.Activa)
            return Fail<(SavingsAccount, SavingsAccount)>(isCashier ? Mensajes.CuentaDestinoInvalida : Mensajes.CuentaInvalida);

        if (source.AccountNumber == destination.AccountNumber)
            return Fail<(SavingsAccount, SavingsAccount)>(isCashier ? Mensajes.OrigenIgualDestinoCajero : Mensajes.DestinoIgualOrigen);

        if (amount <= 0)
            return Fail<(SavingsAccount, SavingsAccount)>(Mensajes.MontoTransferenciaInvalido);

        return ServiceResult<(SavingsAccount, SavingsAccount)>.Ok((source, destination));
    }

    private async Task<ServiceResult<(SavingsAccount Source, SavingsAccount Destination)>> ValidateOwnTransferAsync(
        string ownerUserId, string sourceNumber, string destinationNumber, decimal amount)
    {
        var ownAccounts = await accountRepository.Query()
            .Where(a => a.UserId == ownerUserId && a.Status == ProductStatus.Activa)
            .ToListAsync();

        if (ownAccounts.Count < 2)
            return Fail<(SavingsAccount, SavingsAccount)>(Mensajes.RequiereDosCuentas);

        var source = ownAccounts.FirstOrDefault(a => a.AccountNumber == sourceNumber);
        var destination = ownAccounts.FirstOrDefault(a => a.AccountNumber == destinationNumber);

        if (source is null || destination is null)
            return Fail<(SavingsAccount, SavingsAccount)>(Mensajes.CuentaAhorroNoActiva);

        if (source.AccountNumber == destination.AccountNumber)
            return Fail<(SavingsAccount, SavingsAccount)>(Mensajes.OrigenIgualDestino);

        if (amount <= 0)
            return Fail<(SavingsAccount, SavingsAccount)>(Mensajes.MontoTransferenciaInvalido);

        return ServiceResult<(SavingsAccount, SavingsAccount)>.Ok((source, destination));
    }

    private async Task<ServiceResult<(SavingsAccount Source, SavingsAccount Destination)>> ValidateBeneficiaryTransferAsync(
        string ownerUserId, int beneficiaryId, string sourceNumber, decimal amount)
    {
        var beneficiary = await beneficiaryRepository.Query()
            .Where(b => b.Id == beneficiaryId && b.UserId == ownerUserId)
            .Select(b => new { b.SavingsAccountId })
            .FirstOrDefaultAsync();

        if (beneficiary is null)
            return Fail<(SavingsAccount, SavingsAccount)>(Mensajes.BeneficiarioNoDisponible);

        var destination = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.Id == beneficiary.SavingsAccountId);

        if (destination is null || destination.Status != ProductStatus.Activa)
            return Fail<(SavingsAccount, SavingsAccount)>(Mensajes.BeneficiarioNoDisponible);

        var source = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.AccountNumber == sourceNumber && a.UserId == ownerUserId);

        if (source is null)
            return Fail<(SavingsAccount, SavingsAccount)>(Mensajes.CuentaInvalida);

        if (source.Status != ProductStatus.Activa)
            return Fail<(SavingsAccount, SavingsAccount)>(Mensajes.CuentaAhorroNoActiva);

        if (amount <= 0)
            return Fail<(SavingsAccount, SavingsAccount)>(Mensajes.MontoTransferenciaInvalido);

        return ServiceResult<(SavingsAccount, SavingsAccount)>.Ok((source, destination));
    }

    private async Task<ServiceResult<(SavingsAccount Source, CreditCard Card)>> ValidateCardPaymentAsync(
        string sourceNumber, string cardNumber, decimal amount, string? ownerUserId, bool isCashier)
    {
        var source = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.AccountNumber == sourceNumber);

        if (source is null || (ownerUserId is not null && source.UserId != ownerUserId))
            return Fail<(SavingsAccount, CreditCard)>(Mensajes.CuentaInvalida);

        if (source.Status != ProductStatus.Activa)
            return Fail<(SavingsAccount, CreditCard)>(isCashier ? Mensajes.CuentaInvalida : Mensajes.CuentaAhorroNoActiva);

        var card = await cardRepository.Query()
            .FirstOrDefaultAsync(c => c.CardNumber == cardNumber);

        if (card is null || (ownerUserId is not null && card.UserId != ownerUserId))
            return Fail<(SavingsAccount, CreditCard)>(isCashier ? Mensajes.TarjetaInvalidaCajero : Mensajes.TarjetaNoExiste);

        if (card.Status != ProductStatus.Activa)
            return Fail<(SavingsAccount, CreditCard)>(Mensajes.TarjetaNoActiva);

        if (card.OwedAmount <= 0)
            return Fail<(SavingsAccount, CreditCard)>(Mensajes.TarjetaSinDeuda);

        if (amount <= 0)
            return Fail<(SavingsAccount, CreditCard)>(Mensajes.MontoTransferenciaInvalido);

        return ServiceResult<(SavingsAccount, CreditCard)>.Ok((source, card));
    }

    private async Task<ServiceResult<(SavingsAccount Source, Loan Loan, decimal TotalPending)>> ValidateLoanPaymentAsync(
        string sourceNumber, string loanNumber, decimal amount, string? ownerUserId, bool isCashier)
    {
        var source = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.AccountNumber == sourceNumber);

        if (source is null || (ownerUserId is not null && source.UserId != ownerUserId))
            return Fail<(SavingsAccount, Loan, decimal)>(Mensajes.CuentaInvalida);

        if (source.Status != ProductStatus.Activa)
            return Fail<(SavingsAccount, Loan, decimal)>(isCashier ? Mensajes.CuentaInvalida : Mensajes.CuentaAhorroNoActiva);

        var loan = await loanRepository.Query()
            .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);

        if (loan is null || (ownerUserId is not null && loan.UserId != ownerUserId))
            return Fail<(SavingsAccount, Loan, decimal)>(isCashier ? Mensajes.PrestamoInvalidoCajero : Mensajes.PrestamoNoExiste);

        var totalPending = await installmentRepository.Query()
            .Where(i => i.LoanId == loan.Id)
            .SumAsync(i => i.PendingAmount);

        if (loan.Status != LoanStatus.Activo || totalPending <= 0)
            return Fail<(SavingsAccount, Loan, decimal)>(Mensajes.PrestamoSinCuotasPendientes);

        if (amount <= 0)
            return Fail<(SavingsAccount, Loan, decimal)>(Mensajes.MontoTransferenciaInvalido);

        return ServiceResult<(SavingsAccount, Loan, decimal)>.Ok((source, loan, totalPending));
    }

    // =============================================================
    // Ejecución y helpers
    // =============================================================

    /// <summary>Transferencia genérica: rechazo por fondos registrado, DÉBITO+CRÉDITO transaccional y correos cruzados.</summary>
    private async Task<ServiceResult> ExecuteTransferAsync(
        SavingsAccount source, SavingsAccount destination, decimal amount, string? cashierId,
        string insufficientFundsMessage, bool sendCrossEmails)
    {
        if (source.Balance < amount)
        {
            await RegisterRejectedDebitAsync(source, amount, destination.AccountNumber, source.AccountNumber, cashierId, isPayment: false);
            return ServiceResult.Fail(insufficientFundsMessage);
        }

        await ExecuteApprovedTransferAsync(source, destination, amount, cashierId);

        if (!sendCrossEmails)
            return ServiceResult.Ok();

        // Correo al emisor y al receptor con los asuntos exactos del spec.
        var sender = await accountService.GetByIdAsync(source.UserId);
        var receiver = await accountService.GetByIdAsync(destination.UserId);
        var allSent = true;

        if (sender is not null)
        {
            allSent &= await emailService.SendAsync(new EmailRequest
            {
                To = sender.Email,
                Subject = $"Transacción realizada a la cuenta {LastFour(destination.AccountNumber)}",
                HtmlBody = $"""
                    <h2>Transacción realizada</h2>
                    <p>Se transfirieron <strong>RD${amount:#,##0.00}</strong> desde su cuenta
                    <strong>{source.AccountNumber}</strong> hacia la cuenta terminada en
                    <strong>{LastFour(destination.AccountNumber)}</strong>.</p>
                    """
            });
        }

        if (receiver is not null)
        {
            allSent &= await emailService.SendAsync(new EmailRequest
            {
                To = receiver.Email,
                Subject = $"Transacción enviada desde la cuenta {LastFour(source.AccountNumber)}",
                HtmlBody = $"""
                    <h2>Transacción recibida</h2>
                    <p>Su cuenta <strong>{destination.AccountNumber}</strong> recibió
                    <strong>RD${amount:#,##0.00}</strong> desde la cuenta terminada en
                    <strong>{LastFour(source.AccountNumber)}</strong>.</p>
                    """
            });
        }

        return allSent ? ServiceResult.Ok() : ServiceResult.Ok(Mensajes.TransaccionCorreoFallido);
    }

    /// <summary>DÉBITO en origen + CRÉDITO en destino, todo o nada.</summary>
    private async Task ExecuteApprovedTransferAsync(
        SavingsAccount source, SavingsAccount destination, decimal amount, string? cashierId)
    {
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            source.Balance -= amount;
            destination.Balance += amount;
            accountRepository.Update(source);
            accountRepository.Update(destination);

            var now = DateTime.Now;
            await transactionRepository.AddAsync(new Transaction
            {
                AccountId = source.Id,
                Amount = amount,
                Type = TransactionType.Debito,
                Beneficiary = destination.AccountNumber,
                Origin = source.AccountNumber,
                Status = TransactionStatus.Aprobada,
                CashierId = cashierId,
                CreatedAt = now
            });
            await transactionRepository.AddAsync(new Transaction
            {
                AccountId = destination.Id,
                Amount = amount,
                Type = TransactionType.Credito,
                Beneficiary = destination.AccountNumber,
                Origin = source.AccountNumber,
                Status = TransactionStatus.Aprobada,
                CashierId = cashierId,
                CreatedAt = now
            });
        });
    }

    /// <summary>Rechazo: se registra la transacción RECHAZADA sin tocar balances ni deudas.</summary>
    private async Task RegisterRejectedDebitAsync(
        SavingsAccount source, decimal amount, string beneficiary, string origin, string? cashierId, bool isPayment)
    {
        await transactionRepository.AddAsync(new Transaction
        {
            AccountId = source.Id,
            Amount = amount,
            Type = TransactionType.Debito,
            Beneficiary = beneficiary,
            Origin = origin,
            Status = TransactionStatus.Rechazada,
            CashierId = cashierId,
            IsPayment = isPayment,
            CreatedAt = DateTime.Now
        });
        await unitOfWork.SaveChangesAsync();
    }

    private async Task<TransactionConfirmationDto> BuildTransferConfirmationAsync(
        SavingsAccount source, SavingsAccount destination, decimal amount)
    {
        var users = await accountService.GetByIdsAsync([source.UserId, destination.UserId]);
        return new TransactionConfirmationDto
        {
            SourceAccountNumber = source.AccountNumber,
            SourceHolderFullName = users.GetValueOrDefault(source.UserId)?.FullName,
            DestinationAccountNumber = destination.AccountNumber,
            DestinationHolderFullName = users.GetValueOrDefault(destination.UserId)?.FullName,
            RequestedAmount = amount,
            EffectiveAmount = amount
        };
    }

    /// <summary>Envía la notificación al dueño del producto pagado y al de la cuenta si es distinto.</summary>
    private async Task<bool> SendToOwnersAsync(string productOwnerId, string accountOwnerId, string subject, string body)
    {
        var users = await accountService.GetByIdsAsync([productOwnerId, accountOwnerId]);
        var allSent = true;

        if (users.TryGetValue(productOwnerId, out var productOwner))
            allSent &= await emailService.SendAsync(new EmailRequest { To = productOwner.Email, Subject = subject, HtmlBody = body });

        if (accountOwnerId != productOwnerId && users.TryGetValue(accountOwnerId, out var accountOwner))
            allSent &= await emailService.SendAsync(new EmailRequest { To = accountOwner.Email, Subject = subject, HtmlBody = body });

        return allSent;
    }

    private async Task<SavingsAccount?> GetActiveAccountAsync(string accountNumber)
    {
        var account = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        return account is null || account.Status != ProductStatus.Activa ? null : account;
    }

    private static string LastFour(string number) =>
        number.Length <= 4 ? number : number[^4..];

    private static ServiceResult<T> Fail<T>(string message) => ServiceResult<T>.Fail(message);
}
