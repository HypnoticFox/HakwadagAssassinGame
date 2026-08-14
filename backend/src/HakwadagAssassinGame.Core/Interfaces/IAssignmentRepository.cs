using HakwadagAssassinGame.Core.Entities;

namespace HakwadagAssassinGame.Core.Interfaces;

/// <summary>Provides persistence operations for target assignments.</summary>
public interface IAssignmentRepository
{
    /// <summary>Gets an assignment by identifier.</summary>
    Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets the active assignment for a hunter.</summary>
    Task<Assignment?> GetActiveByHunterIdAsync(Guid gameId, Guid hunterId, CancellationToken cancellationToken = default);

    /// <summary>Gets the most recent assignment for a hunter regardless of status.</summary>
    Task<Assignment?> GetMostRecentByHunterIdAsync(Guid gameId, Guid hunterId, CancellationToken cancellationToken = default);

    /// <summary>Gets assignments for a game.</summary>
    Task<IReadOnlyList<Assignment>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>Adds an assignment.</summary>
    Task AddAsync(Assignment assignment, CancellationToken cancellationToken = default);

    /// <summary>Updates an assignment.</summary>
    Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken = default);
}
