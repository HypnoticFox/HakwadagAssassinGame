using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal sealed class GameEntityTypeConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("Games");

        builder.Property(g => g.StatusId)
            .IsRequired();

        builder
            .HasOne<GameStatus>()
            .WithMany()
            .HasForeignKey(g => g.StatusId);

        builder.Property(g => g.HostApplicationUserId)
            .IsRequired();

        builder
            .HasMany(g => g.Players)
            .WithOne(p => p.Game)
            .HasForeignKey(p => p.GameId);
        
        builder
            .HasOne<Bounty>(g => g.Bounty)
            .WithOne()
            .HasForeignKey<Bounty>(b => b.GameId);

        builder.Property(g => g.AreHighScoresRevealed)
            .IsRequired();

        builder.Property(g => g.Archived)
            .IsRequired();
    }
}