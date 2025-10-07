using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal sealed class GameStatusEntityTypeConfiguration : IEntityTypeConfiguration<GameStatus>
{
    public void Configure(EntityTypeBuilder<GameStatus> builder)
    {
        builder.ToTable("GameStatus");

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

        builder.HasData(GameStatus.ToList());
    }
}