using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal sealed class AssassinGameEntityTypeConfiguraion : IEntityTypeConfiguration<AssassinGame>
{
    public void Configure(EntityTypeBuilder<AssassinGame> builder)
    {
        builder.ToTable("assassin_games");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.StatusId)
            .IsRequired();

        builder
            .HasOne<AssassinGameStatus>()
            .WithMany()
            .HasForeignKey(g => g.StatusId);

        builder.Property(g => g.HostApplicationUserId)
            .IsRequired();

        builder
            .HasMany(g => g.Players)
            .WithOne(p => p.Game)
            .HasForeignKey(p => p.GameId);

        builder
            .HasOne<AssassinGameBounty>(g => g.Bounty)
            .WithOne()
            .HasForeignKey<AssassinGameBounty>(b => b.GameId);

        builder.Property(g => g.AreHighScoresRevealed)
            .IsRequired();

        builder.Property(g => g.Archived)
            .IsRequired();
    }
}