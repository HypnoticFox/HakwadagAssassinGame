using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Infrastructure.Realtime;

/// <summary>Sends game events to clients connected to a game group.</summary>
public interface INotificationHub
{
    /// <summary>Broadcasts a score update.</summary>
    Task ScoreUpdated(string gameId, Guid playerId, int newScore, CancellationToken cancellationToken = default);

    /// <summary>Broadcasts a tag submission.</summary>
    Task TagSubmitted(string gameId, Guid targetId, Guid hunterId, CancellationToken cancellationToken = default);

    /// <summary>Broadcasts a tag resolution.</summary>
    Task TagResolved(string gameId, Guid tagId, TagStatus status, CancellationToken cancellationToken = default);

    /// <summary>Broadcasts that a game has started.</summary>
    Task GameStarted(string gameId, CancellationToken cancellationToken = default);

    /// <summary>Broadcasts that a game has ended.</summary>
    Task GameEnded(string gameId, CancellationToken cancellationToken = default);

    /// <summary>Broadcasts an assignment change.</summary>
    Task AssignmentChanged(string gameId, Guid playerId, CancellationToken cancellationToken = default);

    /// <summary>Broadcasts that a player has left.</summary>
    Task PlayerLeft(string gameId, Guid playerId, CancellationToken cancellationToken = default);
}
