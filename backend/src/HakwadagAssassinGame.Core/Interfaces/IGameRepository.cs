using HakwadagAssassinGame.Core.Entities;

namespace HakwadagAssassinGame.Core.Interfaces;

/// <summary>Provides persistence operations for games.</summary>
public interface IGameRepository
{
    /// <summary>Gets a game by identifier.</summary>
    Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets a game by its invite code.</summary>
    Task<Game?> GetByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken = default);

    /// <summary>Gets all games.</summary>
    Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a game.</summary>
    Task AddAsync(Game game, CancellationToken cancellationToken = default);

    /// <summary>Updates a game.</summary>
    Task UpdateAsync(Game game, CancellationToken cancellationToken = default);

    /// <summary>Deletes a game.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
