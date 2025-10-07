using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal sealed class AssignmentTaskEntityTypeConfiguration : IEntityTypeConfiguration<AssignmentTask>
{
    public void Configure(EntityTypeBuilder<AssignmentTask> builder)
    {
        builder.ToTable("AssignmentTasks");
        
        builder.Property(x => x.Cost)
            .IsRequired();
        
        builder.Property(x => x.Reward)
            .IsRequired();
        
        builder.Property(x => x.Description)
            .IsRequired();
    }
}