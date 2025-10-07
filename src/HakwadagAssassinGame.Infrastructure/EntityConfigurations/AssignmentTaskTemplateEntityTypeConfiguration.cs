using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal sealed class AssignmentTaskTemplateEntityTypeConfiguration : IEntityTypeConfiguration<AssignmentTaskTemplate>
{
    public void Configure(EntityTypeBuilder<AssignmentTaskTemplate> builder)
    {
        builder.ToTable("AssignmentTaskTemplates");
        
        builder.Property(x => x.Cost)
            .IsRequired();
        
        builder.Property(x => x.Reward)
            .IsRequired();
        
        builder.Property(x => x.Description)
            .IsRequired();
    }
}