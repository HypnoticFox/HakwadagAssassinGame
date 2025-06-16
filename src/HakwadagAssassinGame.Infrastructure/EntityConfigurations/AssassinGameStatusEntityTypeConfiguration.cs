using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal sealed class AssassinGameStatusEntityTypeConfiguration : IEntityTypeConfiguration<AssassinGameStatus>
{
    public void Configure(EntityTypeBuilder<AssassinGameStatus> builder)
    {
        builder.ToTable("assassin_game_statuses");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.Property(s => s.Code)
            .IsRequired();

        builder.Property(s => s.Name)
            .IsRequired();

        builder.HasData(AssassinGameStatus.ToList());
    }
}