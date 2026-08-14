using HakwadagAssassinGame.Application.Dtos;

namespace HakwadagAssassinGame.Application.Interfaces;

/// <summary>Sends game events to connected clients.</summary>
public interface IGameEventNotifier
{
    /// <summary>Notifies that a tag was resolved.</summary>
    Task TagResolvedAsync(string gameId, TagSubmissionDto tag, CancellationToken cancellationToken = default);

    /// <summary>Notifies that an assignment changed.</summary>
    Task AssignmentChangedAsync(string gameId, string playerId, CancellationToken cancellationToken = default);

    /// <summary>Notifies that a tag was submitted.</summary>
    Task TagSubmittedAsync(string gameId, TagSubmissionDto tag, CancellationToken cancellationToken = default);

    /// <summary>Notifies that a game has started.</summary>
    Task GameStartedAsync(string gameId, GameDto game, CancellationToken cancellationToken = default);

    /// <summary>Notifies that a game has ended.</summary>
    Task GameEndedAsync(string gameId, GameDto game, CancellationToken cancellationToken = default);

    /// <summary>Notifies that a score was updated.</summary>
    Task ScoreUpdatedAsync(string gameId, CancellationToken cancellationToken = default);

    /// <summary>Notifies that a player left a game.</summary>
    Task PlayerLeftAsync(string gameId, CancellationToken cancellationToken = default);
}
