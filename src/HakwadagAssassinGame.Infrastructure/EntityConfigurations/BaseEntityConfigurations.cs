using HakwadagAssassinGame.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace HakwadagAssassinGame.Infrastructure.EntityConfigurations;

internal static class BaseEntityConfigurations
{
    public static ModelBuilder ApplyBaseEntityConfigurations(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var entityConfiguration = modelBuilder.Entity(entityType.ClrType);

            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                entityConfiguration
                    .HasKey(nameof(Entity.Id));

                entityConfiguration
                    .Property(nameof(Entity.Id))
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
            }

            if (typeof(ITimeStamped).IsAssignableFrom(entityType.ClrType))
                entityConfiguration
                    .Property(nameof(TimeStampedEntity.ConcurrencyToken))
                    .IsConcurrencyToken()
                    .IsRowVersion();
        }

        return modelBuilder;
    }
}