using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.Beneficiary).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Origin).IsRequired().HasMaxLength(50);
        builder.Property(t => t.CashierId).HasMaxLength(450);

        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => new { t.CashierId, t.CreatedAt });
    }
}
