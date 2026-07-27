using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Interfaces;
using HakwadagAssassinGame.Infrastructure.Persistence.Json;
using StackExchange.Redis;

namespace HakwadagAssassinGame.Infrastructure.Persistence.Redis;

/// <summary>Stores game memberships and the game membership index in Redis.</summary>
public sealed class RedisGamePlayerRepository : RedisRepositoryBase, IGamePlayerRepository
{
    /// <summary>Initializes a Redis game membership repository.</summary>
    public RedisGamePlayerRepository(IConnectionMultiplexer connectionMultiplexer)
        : base(connectionMultiplexer)
    {
    }

    /// <inheritdoc />
    public async Task<GamePlayer?> GetAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(MembershipKey(gameId, playerId), cancellationToken);
        return RedisJsonSerializer.Deserialize(value.ToString(), GameJsonContext.Default.GamePlayer);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GamePlayer>> GetByGameIdAsync(
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var playerIds = await GetIdsAsync($"gameplayer:game:{gameId}", cancellationToken);
        var memberships = new List<GamePlayer>(playerIds.Count);
        foreach (var playerId in playerIds)
        {
            var membership = await GetAsync(gameId, playerId, cancellationToken);
            if (membership is not null)
            {
                memberships.Add(membership);
            }
        }

        return memberships;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GamePlayer>> GetByPlayerIdAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var gameIds = await GetIdsAsync($"player:{playerId}:games", cancellationToken);
        var memberships = new List<GamePlayer>(gameIds.Count);
        foreach (var gameId in gameIds)
        {
            var membership = await GetAsync(gameId, playerId, cancellationToken);
            if (membership is not null)
            {
                memberships.Add(membership);
            }
        }

        return memberships;
    }

    /// <inheritdoc />
    public async Task AddAsync(GamePlayer gamePlayer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gamePlayer);
        await SetValueAsync(
            MembershipKey(gamePlayer.GameId, gamePlayer.PlayerId),
            RedisJsonSerializer.Serialize(gamePlayer, GameJsonContext.Default.GamePlayer),
            cancellationToken);
        await AddToSetAsync(
            $"gameplayer:game:{gamePlayer.GameId}",
            gamePlayer.PlayerId.ToString(),
            cancellationToken);
        await AddToSetAsync(
            $"player:{gamePlayer.PlayerId}:games",
            gamePlayer.GameId.ToString(),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(GamePlayer gamePlayer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gamePlayer);
        await AddAsync(gamePlayer, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        await DeleteKeyAsync(MembershipKey(gameId, playerId), cancellationToken);
        await RemoveFromSetAsync($"gameplayer:game:{gameId}", playerId.ToString(), cancellationToken);
        await RemoveFromSetAsync($"player:{playerId}:games", gameId.ToString(), cancellationToken);
    }

    private static string MembershipKey(Guid gameId, Guid playerId) =>
        $"gameplayer:{gameId}:{playerId}";
}
