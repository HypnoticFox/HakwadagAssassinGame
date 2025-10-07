using System.Collections.Immutable;
using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal sealed class PlayerEntityTypeConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");
        
        builder.HasKey(p => p.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.HasOne(x => x.Assignment)
            .WithOne()
            .HasForeignKey<Assignment>(a => a.PlayerUserId);
        
        builder.Property(x => x.GameId)
            .IsRequired();
        
        builder.Property(x => x.Score)
            .IsRequired();
        
        builder.Property(p => p.ApplicationUserId)
            .IsRequired();
    }
}