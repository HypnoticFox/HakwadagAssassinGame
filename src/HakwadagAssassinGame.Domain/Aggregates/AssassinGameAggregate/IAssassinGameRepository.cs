namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public interface IAssassinGameRepository : IRepository<AssassinGame>
{
    AssassinGame Add(AssassinGame applicationUser);
    Task<AssassinGame?> GetAsync(int id, CancellationToken cancellationToken = default);
}