using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Application.Extensions;

namespace ArtemisBankingPro.Core.Application.Services;

public class UserService(
    IAccountService accountService,
    IGenericRepository<SavingsAccount> accountRepository,
    IGenericRepository<Transaction> transactionRepository,
    IProductNumberGenerator numberGenerator,
    IUnitOfWork unitOfWork) : IUserService
{
    public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        if (request.InitialAmount is < 0)
            return ServiceResult<UserDto>.Fail(Mensajes.MontoInicialNegativo);

        var result = await accountService.CreateUserAsync(request);
        if (!result.Succeeded || result.Data is null)
            return result;

        // Cliente o Comercio → cuenta de ahorro Principal automática.
        if (request.Role is Roles.Cliente or Roles.Comercio)
        {
            var initialAmount = request.InitialAmount ?? 0m;
            var accountNumber = await numberGenerator.GenerateAccountOrLoanNumberAsync();

            var account = new SavingsAccount
            {
                AccountNumber = accountNumber,
                Balance = initialAmount,
                Type = AccountType.Principal,
                Status = ProductStatus.Activa,
                UserId = result.Data.Id,
                CreatedAt = DateTime.Now
            };
            // Cuenta + transacción de apertura en una sola transacción de BD.
            // (El usuario Identity vive en otro contexto: ese cruce queda fuera del scope transaccional.)
            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await accountRepository.AddAsync(account);
                await unitOfWork.SaveChangesAsync();

                if (initialAmount > 0)
                {
                    await transactionRepository.AddAsync(new Transaction
                    {
                        AccountId = account.Id,
                        Amount = initialAmount,
                        Type = TransactionType.Credito,
                        Origin = AppConstants.OpeningOrigin,
                        Beneficiary = accountNumber,
                        Status = TransactionStatus.Aprobada,
                        CreatedAt = DateTime.Now
                    });
                    await unitOfWork.SaveChangesAsync();
                }
            });
        }

        return result;
    }

    public async Task<ServiceResult> UpdateUserAsync(UpdateUserRequest request, string actingUserId)
    {
        if (request.Id == actingUserId)
            return ServiceResult.Fail(Mensajes.NoEditarPropiaCuenta);

        if (request.AdditionalAmount is < 0)
            return ServiceResult.Fail(Mensajes.MontoAdicionalNegativo);

        var result = await accountService.UpdateUserAsync(request);
        if (!result.Succeeded)
            return result;

        // Monto adicional > 0 → se suma a la cuenta principal + transacción CRÉDITO.
        if (request.AdditionalAmount is > 0)
        {
            var principal = await accountRepository.Query()
                .FirstOrDefaultAsync(a => a.UserId == request.Id
                    && a.Type == AccountType.Principal
                    && a.Status == ProductStatus.Activa);

            if (principal is not null)
            {
                principal.Balance += request.AdditionalAmount.Value;
                accountRepository.Update(principal);

                await transactionRepository.AddAsync(new Transaction
                {
                    AccountId = principal.Id,
                    Amount = request.AdditionalAmount.Value,
                    Type = TransactionType.Credito,
                    Origin = AppConstants.OpeningOrigin,
                    Beneficiary = principal.AccountNumber,
                    Status = TransactionStatus.Aprobada,
                    CreatedAt = DateTime.Now
                });
                await unitOfWork.SaveChangesAsync();
            }
        }

        return result;
    }

    public Task<ServiceResult> SetUserStatusAsync(string userId, bool active, string actingUserId) =>
        accountService.SetUserStatusAsync(userId, active, actingUserId);

    public Task<PagedResult<UserDto>> GetPagedAsync(int page, string? role, bool onlyCommerce = false)
    {
        var (p, size) = PagedResult<UserDto>.Normalize(page, null);
        return accountService.GetPagedAsync(p, size, role, onlyCommerce);
    }

    public Task<UserDto?> GetByIdAsync(string id) => accountService.GetByIdAsync(id);
}
