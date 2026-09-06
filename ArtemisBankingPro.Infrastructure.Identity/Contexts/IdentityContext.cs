using ArtemisBankingPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Infrastructure.Identity.Contexts;

public class IdentityContext(DbContextOptions<IdentityContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<VerificationToken> VerificationTokens => Set<VerificationToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("Identity");

        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            user.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            user.Property(u => u.Identification).IsRequired().HasMaxLength(20);
            user.HasIndex(u => u.Identification).IsUnique();
        });

        builder.Entity<VerificationToken>(token =>
        {
            token.ToTable("VerificationTokens");
            token.Property(t => t.UserId).IsRequired().HasMaxLength(450);
            token.Property(t => t.Token).IsRequired().HasMaxLength(200);
            token.HasIndex(t => t.Token).IsUnique();
        });
    }
}
