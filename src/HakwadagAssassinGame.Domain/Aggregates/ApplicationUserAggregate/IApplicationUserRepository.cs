namespace HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;

public interface IApplicationUserRepository : IRepository<ApplicationUser>
{
    ApplicationUser Add(ApplicationUser applicationUser);
    Task<ApplicationUser?> GetAsync(string id, CancellationToken cancellationToken = default);
}