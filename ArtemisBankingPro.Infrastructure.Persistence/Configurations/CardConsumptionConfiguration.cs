using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class CardConsumptionConfiguration : IEntityTypeConfiguration<CardConsumption>
{
    public void Configure(EntityTypeBuilder<CardConsumption> builder)
    {
        builder.ToTable("CardConsumptions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Amount).HasPrecision(18, 2);
        builder.Property(c => c.CommerceName).IsRequired().HasMaxLength(150);

        builder.HasOne(c => c.Commerce)
            .WithMany()
            .HasForeignKey(c => c.CommerceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.CommerceId, c.CreatedAt });
    }
}
