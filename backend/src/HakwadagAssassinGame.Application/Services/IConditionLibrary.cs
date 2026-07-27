using HakwadagAssassinGame.Core.Entities.Conditions;

namespace HakwadagAssassinGame.Application.Services;

/// <summary>Stores the condition templates configured for each game.</summary>
public interface IConditionLibrary
{
    /// <summary>Gets templates for a game.</summary>
    Task<IReadOnlyList<Condition>> GetAsync(Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>Adds a condition template to a game.</summary>
    Task AddAsync(Guid gameId, Condition condition, CancellationToken cancellationToken = default);
}
