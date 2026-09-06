using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Infrastructure.Persistence.Contexts;

/// <summary>
/// Contexto de las entidades bancarias. Los usuarios/roles/tokens viven en el
/// IdentityContext (Infrastructure.Identity); aquí solo se referencia el UserId.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SavingsAccount> SavingsAccounts => Set<SavingsAccount>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanInstallment> LoanInstallments => Set<LoanInstallment>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<CardConsumption> CardConsumptions => Set<CardConsumption>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<Commerce> Commerces => Set<Commerce>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
