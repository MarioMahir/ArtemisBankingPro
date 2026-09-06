using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.LoanNumber)
            .IsRequired()
            .HasMaxLength(AppConstants.AccountNumberLength)
            .IsFixedLength();
        builder.HasIndex(l => l.LoanNumber).IsUnique();

        builder.Property(l => l.ApprovedAmount).HasPrecision(18, 2);
        builder.Property(l => l.AnnualInterestRate).HasPrecision(8, 4);
        builder.Property(l => l.UserId).IsRequired().HasMaxLength(450);
        builder.Property(l => l.AdminUserId).IsRequired().HasMaxLength(450);
        builder.HasIndex(l => l.UserId);

        builder.HasMany(l => l.Installments)
            .WithOne(i => i.Loan)
            .HasForeignKey(i => i.LoanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
