using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.ToTable("CreditCards");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CardNumber)
            .IsRequired()
            .HasMaxLength(AppConstants.CardNumberLength)
            .IsFixedLength();
        builder.HasIndex(c => c.CardNumber).IsUnique();

        builder.Property(c => c.CreditLimit).HasPrecision(18, 2);
        builder.Property(c => c.OwedAmount).HasPrecision(18, 2);
        builder.Property(c => c.CvcHash).IsRequired().HasMaxLength(64);
        builder.Property(c => c.UserId).IsRequired().HasMaxLength(450);
        builder.Property(c => c.AdminUserId).IsRequired().HasMaxLength(450);
        builder.HasIndex(c => c.UserId);

        builder.HasMany(c => c.Consumptions)
            .WithOne(x => x.CreditCard)
            .HasForeignKey(x => x.CreditCardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
