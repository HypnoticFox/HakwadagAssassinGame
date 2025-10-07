using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal sealed class AssignmentEntityTypeConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasMany(x => x.Tasks)
            .WithOne()
            .HasForeignKey(x => x.AssignmentId);

        builder.HasOne<AssignmentType>()
            .WithMany()
            .HasForeignKey(x => x.AssignmentTypeId);
        
        builder.Property(x => x.AssignmentTypeId)
            .IsRequired();
    }
}