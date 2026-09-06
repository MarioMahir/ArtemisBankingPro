using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class SavingsAccountConfiguration : IEntityTypeConfiguration<SavingsAccount>
{
    public void Configure(EntityTypeBuilder<SavingsAccount> builder)
    {
        builder.ToTable("SavingsAccounts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AccountNumber)
            .IsRequired()
            .HasMaxLength(AppConstants.AccountNumberLength)
            .IsFixedLength();
        builder.HasIndex(a => a.AccountNumber).IsUnique();

        builder.Property(a => a.Balance).HasPrecision(18, 2);
        builder.Property(a => a.UserId).IsRequired().HasMaxLength(450);
        builder.Property(a => a.AdminUserId).HasMaxLength(450);
        builder.HasIndex(a => a.UserId);

        builder.HasMany(a => a.Transactions)
            .WithOne(t => t.Account)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
