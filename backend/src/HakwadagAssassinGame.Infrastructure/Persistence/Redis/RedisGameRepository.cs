using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Interfaces;
using HakwadagAssassinGame.Infrastructure.Persistence.Json;
using StackExchange.Redis;

namespace HakwadagAssassinGame.Infrastructure.Persistence.Redis;

/// <summary>Stores games and their invite-code index in Redis.</summary>
public sealed class RedisGameRepository : RedisRepositoryBase, IGameRepository
{
    private const string AllGamesKey = "games:all";

    /// <summary>Initializes a Redis game repository.</summary>
    public RedisGameRepository(IConnectionMultiplexer connectionMultiplexer)
        : base(connectionMultiplexer)
    {
    }

    /// <inheritdoc />
    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(Key("game", id), cancellationToken);
        return RedisJsonSerializer.Deserialize(value.ToString(), GameJsonContext.Default.Game);
    }

    /// <inheritdoc />
    public async Task<Game?> GetByInviteCodeAsync(
        string inviteCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inviteCode);
        var idValue = await GetValueAsync($"game:invite:{inviteCode}", cancellationToken);
        return Guid.TryParse(idValue.ToString(), out var id)
            ? await GetByIdAsync(id, cancellationToken)
            : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var ids = await GetIdsAsync(AllGamesKey, cancellationToken);
        var games = new List<Game>(ids.Count);
        foreach (var id in ids)
        {
            var game = await GetByIdAsync(id, cancellationToken);
            if (game is not null)
            {
                games.Add(game);
            }
        }

        return games;
    }

    /// <inheritdoc />
    public async Task AddAsync(Game game, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(game);
        await SetValueAsync(
            Key("game", game.Id),
            RedisJsonSerializer.Serialize(game, GameJsonContext.Default.Game),
            cancellationToken);
        await SetValueAsync($"game:invite:{game.InviteCode}", game.Id.ToString(), cancellationToken);
        await AddToSetAsync(AllGamesKey, game.Id.ToString(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Game game, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(game);
        var existing = await GetByIdAsync(game.Id, cancellationToken);
        if (existing is not null && !string.Equals(existing.InviteCode, game.InviteCode, StringComparison.Ordinal))
        {
            await DeleteKeyAsync($"game:invite:{existing.InviteCode}", cancellationToken);
        }

        await SetValueAsync(
            Key("game", game.Id),
            RedisJsonSerializer.Serialize(game, GameJsonContext.Default.Game),
            cancellationToken);
        await SetValueAsync($"game:invite:{game.InviteCode}", game.Id.ToString(), cancellationToken);
        await AddToSetAsync(AllGamesKey, game.Id.ToString(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        await DeleteKeyAsync(Key("game", id), cancellationToken);
        await RemoveFromSetAsync(AllGamesKey, id.ToString(), cancellationToken);
        if (existing is not null)
        {
            await DeleteKeyAsync($"game:invite:{existing.InviteCode}", cancellationToken);
        }
    }
}
