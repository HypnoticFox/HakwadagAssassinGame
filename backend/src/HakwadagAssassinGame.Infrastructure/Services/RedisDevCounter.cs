using HakwadagAssassinGame.Application.Interfaces;
using StackExchange.Redis;

namespace HakwadagAssassinGame.Infrastructure.Services;

/// <summary>Stores development counters in Redis.</summary>
public sealed class RedisDevCounter : IDevCounter
{
    private readonly IDatabase database;

    /// <summary>Initializes a dev counter.</summary>
    public RedisDevCounter(IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        database = connectionMultiplexer.GetDatabase();
    }

    /// <inheritdoc />
    public async Task<long> IncrementAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        cancellationToken.ThrowIfCancellationRequested();
        var value = await database.StringIncrementAsync($"dev:counter:{name}").WaitAsync(cancellationToken);
        return value;
    }
}
