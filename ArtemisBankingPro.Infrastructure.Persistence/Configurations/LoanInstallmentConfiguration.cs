using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class LoanInstallmentConfiguration : IEntityTypeConfiguration<LoanInstallment>
{
    public void Configure(EntityTypeBuilder<LoanInstallment> builder)
    {
        builder.ToTable("LoanInstallments");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Value).HasPrecision(18, 2);
        builder.Property(i => i.InterestAmount).HasPrecision(18, 2);
        builder.Property(i => i.PrincipalAmount).HasPrecision(18, 2);
        builder.Property(i => i.PendingAmount).HasPrecision(18, 2);

        builder.HasIndex(i => new { i.LoanId, i.Number }).IsUnique();
        builder.HasIndex(i => new { i.DueDate, i.Status });
    }
}
