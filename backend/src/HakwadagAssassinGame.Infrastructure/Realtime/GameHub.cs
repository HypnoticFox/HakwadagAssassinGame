using Microsoft.AspNetCore.SignalR;
using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Infrastructure.Realtime;

/// <summary>SignalR hub for real-time game updates.</summary>
public sealed class GameHub : Hub
{
    /// <summary>Adds the current connection to a game group.</summary>
    public async Task JoinGame(string gameId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId, cancellationToken);
    }

    /// <summary>Removes the current connection from a game group.</summary>
    public async Task LeaveGame(string gameId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId, cancellationToken);
    }

    /// <summary>Broadcasts a score update to a game.</summary>
    public Task ScoreUpdated(
        string gameId,
        Guid playerId,
        int newScore,
        CancellationToken cancellationToken = default) =>
        Clients.Group(gameId).SendAsync(nameof(ScoreUpdated), playerId, newScore, cancellationToken);

    /// <summary>Broadcasts a submitted tag to a game.</summary>
    public Task TagSubmitted(
        string gameId,
        Guid targetId,
        Guid hunterId,
        CancellationToken cancellationToken = default) =>
        Clients.Group(gameId).SendAsync(nameof(TagSubmitted), targetId, hunterId, cancellationToken);

    /// <summary>Broadcasts a resolved tag to a game.</summary>
    public Task TagResolved(
        string gameId,
        Guid tagId,
        TagStatus status,
        CancellationToken cancellationToken = default) =>
        Clients.Group(gameId).SendAsync(nameof(TagResolved), tagId, status, cancellationToken);

    /// <summary>Broadcasts that a game has started.</summary>
    public Task GameStarted(string gameId, CancellationToken cancellationToken = default) =>
        Clients.Group(gameId).SendAsync(nameof(GameStarted), cancellationToken);

    /// <summary>Broadcasts that a game has ended.</summary>
    public Task GameEnded(string gameId, CancellationToken cancellationToken = default) =>
        Clients.Group(gameId).SendAsync(nameof(GameEnded), cancellationToken);

    /// <summary>Broadcasts an assignment change to a game.</summary>
    public Task AssignmentChanged(
        string gameId,
        Guid playerId,
        CancellationToken cancellationToken = default) =>
        Clients.Group(gameId).SendAsync(nameof(AssignmentChanged), playerId, cancellationToken);

    /// <summary>Broadcasts that a player left a game.</summary>
    public Task PlayerLeft(
        string gameId,
        Guid playerId,
        CancellationToken cancellationToken = default) =>
        Clients.Group(gameId).SendAsync(nameof(PlayerLeft), playerId, cancellationToken);
}
