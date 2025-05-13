using HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal sealed class ApplicationUserEntityTypeConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("application_user");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .IsRequired();
    }
}