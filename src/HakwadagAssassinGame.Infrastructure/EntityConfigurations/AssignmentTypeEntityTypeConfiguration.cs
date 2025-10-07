using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal sealed class AssignmentTypeEntityTypeConfiguration : IEntityTypeConfiguration<AssignmentType>
{
    public void Configure(EntityTypeBuilder<AssignmentType> builder)
    {
        builder.ToTable("AssignmentTypes");

        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => x.Code)
            .IsUnique();
        
        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .IsRequired();
        
        builder.Property(x => x.Code)
            .IsRequired();
        
        builder.Property(x => x.Name)
            .IsRequired();

        builder.HasData(AssignmentType.ToList());
    }
}