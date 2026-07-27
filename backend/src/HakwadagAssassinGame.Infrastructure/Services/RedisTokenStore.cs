using HakwadagAssassinGame.Application.Interfaces;
using StackExchange.Redis;

namespace HakwadagAssassinGame.Infrastructure.Services;

/// <summary>Stores temporary authentication tokens in Redis.</summary>
public sealed class RedisTokenStore : ITokenStore
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(7);
    private readonly IDatabase database;

    /// <summary>Initializes a token store.</summary>
    public RedisTokenStore(IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        database = connectionMultiplexer.GetDatabase();
    }

    /// <inheritdoc />
    public async Task StoreAsync(
        string token,
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        cancellationToken.ThrowIfCancellationRequested();
        await database.StringSetAsync($"token:{token}", playerId.ToString(), TokenLifetime)
            .WaitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid?> GetPlayerIdAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        cancellationToken.ThrowIfCancellationRequested();
        var value = await database.StringGetAsync($"token:{token}").WaitAsync(cancellationToken);
        return Guid.TryParse(value.ToString(), out var playerId) ? playerId : null;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        cancellationToken.ThrowIfCancellationRequested();
        await database.KeyDeleteAsync($"token:{token}").WaitAsync(cancellationToken);
    }
}
