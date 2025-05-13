namespace HakwadagAssassinGame.Domain.SeedWork;

public interface IRepository<T> where T : IAggregateRoot
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}