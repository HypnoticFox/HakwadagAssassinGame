using HakwadagAssassinGame.Core.Entities;

namespace HakwadagAssassinGame.Core.Interfaces;

/// <summary>Provides persistence operations for game memberships.</summary>
public interface IGamePlayerRepository
{
    /// <summary>Gets a membership by game and player identifiers.</summary>
    Task<GamePlayer?> GetAsync(Guid gameId, Guid playerId, CancellationToken cancellationToken = default);

    /// <summary>Gets all memberships for a game.</summary>
    Task<IReadOnlyList<GamePlayer>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>Gets all memberships for a player.</summary>
    Task<IReadOnlyList<GamePlayer>> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default);

    /// <summary>Adds a membership.</summary>
    Task AddAsync(GamePlayer gamePlayer, CancellationToken cancellationToken = default);

    /// <summary>Updates a membership.</summary>
    Task UpdateAsync(GamePlayer gamePlayer, CancellationToken cancellationToken = default);

    /// <summary>Removes a player from a game.</summary>
    Task RemoveAsync(Guid gameId, Guid playerId, CancellationToken cancellationToken = default);
}
