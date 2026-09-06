using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Beneficiaries;
using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Application.Extensions;

namespace ArtemisBankingPro.Core.Application.Services;

public class BeneficiaryService(
    IGenericRepository<Beneficiary> beneficiaryRepository,
    IGenericRepository<SavingsAccount> accountRepository,
    IAccountService accountService,
    IUnitOfWork unitOfWork) : IBeneficiaryService
{
    public async Task<List<BeneficiaryDto>> GetByUserAsync(string userId)
    {
        var rows = await beneficiaryRepository.Query()
            .Where(b => b.UserId == userId)
            .Select(b => new
            {
                b.Id,
                b.CreatedAt,
                b.SavingsAccount.AccountNumber,
                HolderId = b.SavingsAccount.UserId
            })
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        if (rows.Count == 0) return [];

        // Nombre y apellido del titular se obtienen de Identity.
        var holders = await accountService.GetByIdsAsync(rows.Select(r => r.HolderId));

        return rows.Select(r => new BeneficiaryDto
        {
            Id = r.Id,
            AccountNumber = r.AccountNumber,
            HolderFirstName = holders.TryGetValue(r.HolderId, out var u) ? u.FirstName : string.Empty,
            HolderLastName = holders.TryGetValue(r.HolderId, out var v) ? v.LastName : string.Empty,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<ServiceResult> AddAsync(string userId, string accountNumber)
    {
        var number = accountNumber?.Trim() ?? string.Empty;
        if (number.Length != AppConstants.AccountNumberLength || !number.All(char.IsDigit))
            return ServiceResult.Fail(Mensajes.CuentaInvalida);

        var account = await accountRepository.Query()
            .FirstOrDefaultAsync(a => a.AccountNumber == number);
        if (account is null)
            return ServiceResult.Fail(Mensajes.CuentaInvalida);

        if (account.Status == ProductStatus.Cancelada)
            return ServiceResult.Fail(Mensajes.BeneficiarioCancelado);

        if (account.UserId == userId)
            return ServiceResult.Fail(Mensajes.BeneficiarioPropio);

        var alreadyExists = await beneficiaryRepository.Query()
            .AnyAsync(b => b.UserId == userId && b.SavingsAccountId == account.Id);
        if (alreadyExists)
            return ServiceResult.Fail(Mensajes.BeneficiarioDuplicado);

        await beneficiaryRepository.AddAsync(new Beneficiary
        {
            UserId = userId,
            SavingsAccountId = account.Id,
            CreatedAt = DateTime.Now
        });
        await unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok(Mensajes.BeneficiarioAgregado);
    }

    public async Task<ServiceResult> RemoveAsync(string userId, int beneficiaryId)
    {
        var beneficiary = await beneficiaryRepository.Query()
            .FirstOrDefaultAsync(b => b.Id == beneficiaryId && b.UserId == userId);
        if (beneficiary is null)
            return ServiceResult.Fail(Mensajes.BeneficiarioNoDisponible);

        beneficiaryRepository.Delete(beneficiary);
        await unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok(Mensajes.BeneficiarioEliminado);
    }
}
