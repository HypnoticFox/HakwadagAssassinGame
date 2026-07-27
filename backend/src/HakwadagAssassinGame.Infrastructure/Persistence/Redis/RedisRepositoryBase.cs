using StackExchange.Redis;

namespace HakwadagAssassinGame.Infrastructure.Persistence.Redis;

/// <summary>Provides common Redis access helpers for infrastructure repositories.</summary>
public abstract class RedisRepositoryBase
{
    protected RedisRepositoryBase(IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        Database = connectionMultiplexer.GetDatabase();
    }

    /// <summary>Gets the Redis database used by the repository.</summary>
    protected IDatabase Database { get; }

    protected static string Key(string prefix, Guid id) => $"{prefix}:{id}";

    protected async Task<RedisValue> GetValueAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Database.StringGetAsync(key).WaitAsync(cancellationToken);
    }

    protected async Task SetValueAsync(
        string key,
        RedisValue value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Database.StringSetAsync(key, value).WaitAsync(cancellationToken);
    }

    protected async Task DeleteKeyAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Database.KeyDeleteAsync(key).WaitAsync(cancellationToken);
    }

    protected async Task AddToSetAsync(
        string key,
        RedisValue value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Database.SetAddAsync(key, value).WaitAsync(cancellationToken);
    }

    protected async Task RemoveFromSetAsync(
        string key,
        RedisValue value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Database.SetRemoveAsync(key, value).WaitAsync(cancellationToken);
    }

    protected async Task<IReadOnlyList<Guid>> GetIdsAsync(
        string key,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var values = await Database.SetMembersAsync(key).WaitAsync(cancellationToken);
        var ids = new List<Guid>(values.Length);
        foreach (var value in values)
        {
            if (Guid.TryParse(value.ToString(), out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }
}
