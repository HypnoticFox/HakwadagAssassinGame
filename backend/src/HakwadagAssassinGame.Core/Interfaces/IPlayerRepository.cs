using HakwadagAssassinGame.Core.Entities;

namespace HakwadagAssassinGame.Core.Interfaces;

/// <summary>Provides persistence operations for players.</summary>
public interface IPlayerRepository
{
    /// <summary>Gets a player by identifier.</summary>
    Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets a player by email address.</summary>
    Task<Player?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Adds a player.</summary>
    Task AddAsync(Player player, CancellationToken cancellationToken = default);

    /// <summary>Updates a player.</summary>
    Task UpdateAsync(Player player, CancellationToken cancellationToken = default);

    /// <summary>Deletes a player.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
