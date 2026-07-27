using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Interfaces;
using HakwadagAssassinGame.Infrastructure.Persistence.Json;
using StackExchange.Redis;

namespace HakwadagAssassinGame.Infrastructure.Persistence.Redis;

/// <summary>Stores players and their email index in Redis.</summary>
public sealed class RedisPlayerRepository : RedisRepositoryBase, IPlayerRepository
{
    /// <summary>Initializes a Redis player repository.</summary>
    public RedisPlayerRepository(IConnectionMultiplexer connectionMultiplexer)
        : base(connectionMultiplexer)
    {
    }

    /// <inheritdoc />
    public async Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(Key("player", id), cancellationToken);
        return RedisJsonSerializer.Deserialize(value.ToString(), GameJsonContext.Default.Player);
    }

    /// <inheritdoc />
    public async Task<Player?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        var idValue = await GetValueAsync($"player:email:{email}", cancellationToken);
        return Guid.TryParse(idValue.ToString(), out var id)
            ? await GetByIdAsync(id, cancellationToken)
            : null;
    }

    /// <inheritdoc />
    public async Task AddAsync(Player player, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(player);
        await SetValueAsync(
            Key("player", player.Id),
            RedisJsonSerializer.Serialize(player, GameJsonContext.Default.Player),
            cancellationToken);
        await SetValueAsync($"player:email:{player.Email}", player.Id.ToString(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Player player, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(player);
        var existing = await GetByIdAsync(player.Id, cancellationToken);
        if (existing is not null && !string.Equals(existing.Email, player.Email, StringComparison.Ordinal))
        {
            await DeleteKeyAsync($"player:email:{existing.Email}", cancellationToken);
        }

        await SetValueAsync(
            Key("player", player.Id),
            RedisJsonSerializer.Serialize(player, GameJsonContext.Default.Player),
            cancellationToken);
        await SetValueAsync($"player:email:{player.Email}", player.Id.ToString(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        await DeleteKeyAsync(Key("player", id), cancellationToken);
        if (existing is not null)
        {
            await DeleteKeyAsync($"player:email:{existing.Email}", cancellationToken);
        }
    }
}
