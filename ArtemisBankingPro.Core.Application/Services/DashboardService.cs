using ArtemisBankingPro.Core.Application.Dtos.Dashboard;
using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Core.Application.Extensions;

namespace ArtemisBankingPro.Core.Application.Services;

public class DashboardService(
    IGenericRepository<Transaction> transactionRepository,
    IGenericRepository<SavingsAccount> accountRepository,
    IGenericRepository<Loan> loanRepository,
    IGenericRepository<CreditCard> cardRepository,
    IAccountService accountService,
    ILoanService loanService) : IDashboardService
{
    public async Task<DashboardDto> GetAdminDashboardAsync()
    {
        var today = DateTime.Now.Date;
        var tomorrow = today.AddDays(1);
        var transactions = transactionRepository.Query();

        var totalTransactions = await transactions.CountAsync();
        var transactionsToday = await transactions
            .CountAsync(t => t.CreatedAt >= today && t.CreatedAt < tomorrow);

        // Pagos: SOLO pagos a tarjeta + pagos a préstamo.
        var totalPayments = await transactions.CountAsync(t => t.IsPayment);
        var paymentsToday = await transactions
            .CountAsync(t => t.IsPayment && t.CreatedAt >= today && t.CreatedAt < tomorrow);

        var activeClients = await accountService.CountByRoleAndStatusAsync(Roles.Cliente, true);
        var inactiveClients = await accountService.CountByRoleAndStatusAsync(Roles.Cliente, false);

        var activeAccounts = await accountRepository.Query()
            .CountAsync(a => a.Status == ProductStatus.Activa);
        var activeLoans = await loanRepository.Query()
            .CountAsync(l => l.Status == LoanStatus.Activo);
        var activeCards = await cardRepository.Query()
            .CountAsync(c => c.Status == ProductStatus.Activa);

        return new DashboardDto
        {
            TotalTransactions = totalTransactions,
            TransactionsToday = transactionsToday,
            TotalPayments = totalPayments,
            PaymentsToday = paymentsToday,
            ActiveClients = activeClients,
            InactiveClients = inactiveClients,
            TotalActiveProducts = activeAccounts + activeLoans + activeCards,
            ActiveLoans = activeLoans,
            ActiveCreditCards = activeCards,
            ActiveSavingsAccounts = activeAccounts,
            AverageDebtPerClient = await loanService.GetAverageDebtAsync()
        };
    }

    public async Task<CashierDashboardDto> GetCashierDashboardAsync(string cashierId)
    {
        var today = DateTime.Now.Date;
        var tomorrow = today.AddDays(1);

        // SOLO operaciones del cajero autenticado y SOLO de hoy.
        var own = transactionRepository.Query()
            .Where(t => t.CashierId == cashierId && t.CreatedAt >= today && t.CreatedAt < tomorrow);

        return new CashierDashboardDto
        {
            TransactionsToday = await own.CountAsync(),
            PaymentsToday = await own.CountAsync(t => t.IsPayment),
            DepositsToday = await own.CountAsync(t => t.Origin == AppConstants.DepositOrigin),
            WithdrawalsToday = await own.CountAsync(t => t.Beneficiary == AppConstants.WithdrawalBeneficiary)
        };
    }
}
