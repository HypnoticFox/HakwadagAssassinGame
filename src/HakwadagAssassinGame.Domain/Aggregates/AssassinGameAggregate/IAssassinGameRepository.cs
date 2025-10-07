namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public interface IAssassinGameRepository : IRepository<Game>
{
    Game Add(Game applicationUser);
    Task<Game?> GetAsync(int id, CancellationToken cancellationToken = default);
}