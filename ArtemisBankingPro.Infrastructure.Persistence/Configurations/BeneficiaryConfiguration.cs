using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
{
    public void Configure(EntityTypeBuilder<Beneficiary> builder)
    {
        builder.ToTable("Beneficiaries");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.UserId).IsRequired().HasMaxLength(450);

        // Un cliente no puede registrar dos veces la misma cuenta beneficiaria.
        builder.HasIndex(b => new { b.UserId, b.SavingsAccountId }).IsUnique();

        builder.HasOne(b => b.SavingsAccount)
            .WithMany()
            .HasForeignKey(b => b.SavingsAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
