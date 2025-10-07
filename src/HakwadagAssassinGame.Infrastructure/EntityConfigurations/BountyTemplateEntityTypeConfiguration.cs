using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal sealed class BountyTemplateEntityTypeConfiguration : IEntityTypeConfiguration<BountyTemplate>
{
    public void Configure(EntityTypeBuilder<BountyTemplate> builder)
    {
        builder.ToTable("BountyTemplates");
        
        builder.Property(x => x.Description)
            .IsRequired();
    }
}