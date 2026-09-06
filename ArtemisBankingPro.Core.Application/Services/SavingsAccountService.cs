using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Application.Dtos.Transactions;
using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using AutoMapper;
using ArtemisBankingPro.Core.Application.Extensions;

namespace ArtemisBankingPro.Core.Application.Services;

public class SavingsAccountService(
    IGenericRepository<SavingsAccount> accountRepository,
    IGenericRepository<Transaction> transactionRepository,
    IProductNumberGenerator numberGenerator,
    IAccountService accountService,
    IUnitOfWork unitOfWork,
    IMapper mapper) : ISavingsAccountService
{
    public async Task<ServiceResult<PagedResult<SavingsAccountDto>>> GetPagedAsync(
        int page, ProductStatus? status, AccountType? type, string? identification)
    {
        var (p, size) = PagedResult<SavingsAccountDto>.Normalize(page, null);
        var query = accountRepository.Query();

        if (!string.IsNullOrWhiteSpace(identification))
        {
            var client = await accountService.GetByIdentificationAsync(identification.Trim());
            if (client is null)
                return ServiceResult<PagedResult<SavingsAccountDto>>.Fail(Mensajes.ClienteNoExistePorCedula);

            query = query.Where(a => a.UserId == client.Id);
        }

        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var total = await query.CountAsync();

        // Sin filtro de estado: activas primero, luego canceladas; cada grupo por recencia.
        var ordered = status.HasValue
            ? query.OrderByDescending(a => a.CreatedAt)
            : query.OrderBy(a => a.Status).ThenByDescending(a => a.CreatedAt);

        var items = await ordered.Skip((p - 1) * size).Take(size).ToListAsync();
        var dtos = items.Select(mapper.Map<SavingsAccountDto>).ToList();
        await EnrichWithHoldersAsync(dtos);

        return ServiceResult<PagedResult<SavingsAccountDto>>.Ok(new PagedResult<SavingsAccountDto>
        {
            Items = dtos,
            Page = p,
            PageSize = size,
            TotalCount = total
        });
    }

    public async Task<ServiceResult<SavingsAccountDto>> CreateSecondaryAsync(
        string clientId, decimal? initialBalance, string adminUserId)
    {
        var client = await accountService.GetByIdAsync(clientId);
        if (client is null || !client.IsActive || client.Role != Roles.Cliente)
            return ServiceResult<SavingsAccountDto>.Fail(Mensajes.SoloCuentasClientesActivos);

        var hasPrincipal = await accountRepository.Query()
            .AnyAsync(a => a.UserId == clientId
                && a.Type == AccountType.Principal
                && a.Status == ProductStatus.Activa);
        if (!hasPrincipal)
            return ServiceResult<SavingsAccountDto>.Fail(Mensajes.RequierePrincipalActiva);

        if (initialBalance is < 0)
            return ServiceResult<SavingsAccountDto>.Fail(Mensajes.BalanceInicialNegativo);

        var balance = initialBalance ?? 0m;
        var accountNumber = await numberGenerator.GenerateAccountOrLoanNumberAsync();

        var account = new SavingsAccount
        {
            AccountNumber = accountNumber,
            Balance = balance,
            Type = AccountType.Secundaria,
            Status = ProductStatus.Activa,
            UserId = clientId,
            AdminUserId = adminUserId,
            CreatedAt = DateTime.Now
        };
        // Cuenta + transacción de apertura en una sola transacción de BD.
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await accountRepository.AddAsync(account);
            await unitOfWork.SaveChangesAsync();

            if (balance > 0)
            {
                await transactionRepository.AddAsync(new Transaction
                {
                    AccountId = account.Id,
                    Amount = balance,
                    Type = TransactionType.Credito,
                    Origin = AppConstants.OpeningOrigin,
                    Beneficiary = accountNumber,
                    Status = TransactionStatus.Aprobada,
                    CreatedAt = DateTime.Now
                });
                await unitOfWork.SaveChangesAsync();
            }
        });

        return ServiceResult<SavingsAccountDto>.Ok(mapper.Map<SavingsAccountDto>(account));
    }

    public async Task<ServiceResult> CancelAsync(string accountNumber)
    {
        var account = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

        if (account is null)
            return ServiceResult.Fail(Mensajes.CuentaNoExiste);

        if (account.Status == ProductStatus.Cancelada)
            return ServiceResult.Fail(Mensajes.CuentaYaCancelada);

        if (account.Type == AccountType.Principal)
            return ServiceResult.Fail(Mensajes.PrincipalNoCancelable);

        var principal = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.UserId == account.UserId
                && a.Type == AccountType.Principal
                && a.Status == ProductStatus.Activa);
        if (principal is null)
            return ServiceResult.Fail(Mensajes.SinPrincipalParaFondos);

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Si hay balance, transferirlo a la principal ANTES de cancelar.
            if (account.Balance > 0)
            {
                var amount = account.Balance;

                await transactionRepository.AddAsync(new Transaction
                {
                    AccountId = account.Id,
                    Amount = amount,
                    Type = TransactionType.Debito,
                    Origin = account.AccountNumber,
                    Beneficiary = principal.AccountNumber,
                    Status = TransactionStatus.Aprobada,
                    CreatedAt = DateTime.Now
                });
                await transactionRepository.AddAsync(new Transaction
                {
                    AccountId = principal.Id,
                    Amount = amount,
                    Type = TransactionType.Credito,
                    Origin = account.AccountNumber,
                    Beneficiary = principal.AccountNumber,
                    Status = TransactionStatus.Aprobada,
                    CreatedAt = DateTime.Now
                });

                principal.Balance += amount;
                account.Balance = 0;
                accountRepository.Update(principal);
            }

            account.Status = ProductStatus.Cancelada;
            accountRepository.Update(account);
        });

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<PagedResult<TransactionDto>>> GetTransactionsAsync(string accountNumber, int page)
    {
        var account = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        if (account is null)
            return ServiceResult<PagedResult<TransactionDto>>.Fail(Mensajes.CuentaNoExiste);

        var (p, size) = PagedResult<TransactionDto>.Normalize(page, null);
        var query = transactionRepository.Query().Where(t => t.AccountId == account.Id);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((p - 1) * size)
            .Take(size)
            .ToListAsync();

        return ServiceResult<PagedResult<TransactionDto>>.Ok(new PagedResult<TransactionDto>
        {
            Items = items.Select(t => ToDto(t, account.AccountNumber)).ToList(),
            Page = p,
            PageSize = size,
            TotalCount = total
        });
    }

    public async Task<List<SavingsAccountDto>> GetActiveAccountsByUserAsync(string userId)
    {
        var accounts = await accountRepository.Query()
            .Where(a => a.UserId == userId && a.Status == ProductStatus.Activa)
            .OrderBy(a => a.Type)               // Principal (1) primero
            .ThenByDescending(a => a.Balance)   // secundarias por balance descendente
            .ToListAsync();

        return accounts.Select(mapper.Map<SavingsAccountDto>).ToList();
    }

    public async Task<SavingsAccountDto?> GetByNumberAsync(string accountNumber)
    {
        var account = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        if (account is null) return null;

        var dto = mapper.Map<SavingsAccountDto>(account);
        await EnrichWithHoldersAsync([dto]);
        return dto;
    }

    // ---------------- Helpers ----------------

    private async Task EnrichWithHoldersAsync(List<SavingsAccountDto> dtos)
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

    // TransactionDto necesita el número de cuenta (fuente externa): se mantiene manual.
    private static TransactionDto ToDto(Transaction transaction, string accountNumber) => new()
    {
        Id = transaction.Id,
        AccountNumber = accountNumber,
        Amount = transaction.Amount,
        Type = transaction.Type,
        Beneficiary = transaction.Beneficiary,
        Origin = transaction.Origin,
        Status = transaction.Status,
        CashierId = transaction.CashierId,
        IsPayment = transaction.IsPayment,
        CreatedAt = transaction.CreatedAt
    };
}
