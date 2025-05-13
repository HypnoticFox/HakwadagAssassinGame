using HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;
using HakwadagAssassinGame.Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace HakwadagAssassinGame.Infrastructure;

public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    internal DbSet<ApplicationUser> Users { get; set; }
    // internal DbSet<AssassinGame> Games { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyBaseEntityConfigurations();

        modelBuilder.ApplyConfiguration(new ApplicationUserEntityTypeConfiguration());
    }
}