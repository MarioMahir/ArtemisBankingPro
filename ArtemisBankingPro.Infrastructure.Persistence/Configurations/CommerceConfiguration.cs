using ArtemisBankingPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBankingPro.Infrastructure.Persistence.Configurations;

public class CommerceConfiguration : IEntityTypeConfiguration<Commerce>
{
    public void Configure(EntityTypeBuilder<Commerce> builder)
    {
        builder.ToTable("Commerces");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
        builder.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Rnc).IsRequired().HasMaxLength(20);

        builder.HasIndex(c => c.Rnc).IsUnique();
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
