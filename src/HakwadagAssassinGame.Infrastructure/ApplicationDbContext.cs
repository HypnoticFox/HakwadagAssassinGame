using HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;
using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using HakwadagAssassinGame.Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace HakwadagAssassinGame.Infrastructure;

public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    internal DbSet<ApplicationUser> Users { get; set; }
    internal DbSet<Game> Games { get; set; }
    internal DbSet<GameStatus> GameStatuses { get; set; }
    internal DbSet<Player> GamePlayers { get; set; }
    internal DbSet<Assignment> Assignments { get; set; }
    internal DbSet<AssignmentTask> AssignmentTasks { get; set; }
    internal DbSet<AssignmentType> AssignmentTypes { get; set; }
    internal DbSet<AssignmentTaskTemplate> AssignmentTaskTemplates { get; set; }
    internal DbSet<Bounty> Bounties { get; set; }
    internal DbSet<BountyTemplate> BountyTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyBaseEntityConfigurations();

        modelBuilder.ApplyConfiguration(new ApplicationUserEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new GameEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new GameStatusEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PlayerEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new BountyEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AssignmentEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AssignmentTaskEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AssignmentTypeEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AssignmentTaskTemplateEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new BountyTemplateEntityTypeConfiguration());
    }
}