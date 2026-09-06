using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class ProductNumberRepository(AppDbContext context) : IProductNumberRepository
{
    public async Task<bool> AccountOrLoanNumberExistsAsync(string number) =>
        await context.SavingsAccounts.AnyAsync(a => a.AccountNumber == number)
        || await context.Loans.AnyAsync(l => l.LoanNumber == number);

    public Task<bool> CardNumberExistsAsync(string number) =>
        context.CreditCards.AnyAsync(c => c.CardNumber == number);
}
