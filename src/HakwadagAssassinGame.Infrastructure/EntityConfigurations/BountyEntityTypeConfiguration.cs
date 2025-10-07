using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal sealed class BountyEntityTypeConfiguration : IEntityTypeConfiguration<Bounty>
{
    public void Configure(EntityTypeBuilder<Bounty> builder)
    {
        builder.ToTable("Bounties");
        
        builder.Property(x => x.GameId)
            .IsRequired();
        
        builder.Property(x => x.TargetId)
            .IsRequired();
        
        builder.Property(x => x.Reward)
            .IsRequired();
        
        builder.Property(x => x.Description)
            .IsRequired();
    }
}