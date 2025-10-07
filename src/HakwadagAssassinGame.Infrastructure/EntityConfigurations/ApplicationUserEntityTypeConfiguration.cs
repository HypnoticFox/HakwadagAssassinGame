using HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal sealed class ApplicationUserEntityTypeConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUsers");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .IsRequired();
    }
}