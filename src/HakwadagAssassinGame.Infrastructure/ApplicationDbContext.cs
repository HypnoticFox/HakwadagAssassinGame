using HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;
using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using HakwadagAssassinGame.Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace HakwadagAssassinGame.Infrastructure;

public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    internal DbSet<ApplicationUser> Users { get; set; }
    internal DbSet<AssassinGame> Games { get; set; }
    internal DbSet<AssassinGameStatus> GameStatuses { get; set; }
    internal DbSet<AssassinGamePlayer> GamePlayers { get; set; }
    internal DbSet<AssassinGameAssignment> Assignments { get; set; }
    internal DbSet<AssassinGameAssignmentTask> AssignmentTasks { get; set; }
    internal DbSet<AssassinGameAssignmentType> AssignmentTypes { get; set; }
    internal DbSet<AssassinGameAssignmentTaskTemplate> AssignmentTemplates { get; set; }
    internal DbSet<AssassinGameBounty> Bounties { get; set; }
    internal DbSet<AssassinGameBountyTemplate> BountyTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyBaseEntityConfigurations();

        modelBuilder.ApplyConfiguration(new ApplicationUserEntityTypeConfiguration());
    }
}