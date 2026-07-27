using HakwadagAssassinGame.Core.Enums;
using Microsoft.AspNetCore.SignalR;

namespace HakwadagAssassinGame.Infrastructure.Realtime;

/// <summary>SignalR-backed implementation of the application notification abstraction.</summary>
public sealed class SignalRNotificationHub : INotificationHub
{
    private readonly IHubContext<GameHub> hubContext;

    /// <summary>Initializes a SignalR notification hub.</summary>
    public SignalRNotificationHub(IHubContext<GameHub> hubContext)
    {
        this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    /// <inheritdoc />
    public Task ScoreUpdated(
        string gameId,
        Guid playerId,
        int newScore,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(gameId).SendAsync(nameof(GameHub.ScoreUpdated), playerId, newScore, cancellationToken);

    /// <inheritdoc />
    public Task TagSubmitted(
        string gameId,
        Guid targetId,
        Guid hunterId,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(gameId).SendAsync(nameof(GameHub.TagSubmitted), targetId, hunterId, cancellationToken);

    /// <inheritdoc />
    public Task TagResolved(
        string gameId,
        Guid tagId,
        TagStatus status,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(gameId).SendAsync(nameof(GameHub.TagResolved), tagId, status, cancellationToken);

    /// <inheritdoc />
    public Task GameStarted(string gameId, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(gameId).SendAsync(nameof(GameHub.GameStarted), cancellationToken);

    /// <inheritdoc />
    public Task GameEnded(string gameId, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(gameId).SendAsync(nameof(GameHub.GameEnded), cancellationToken);

    /// <inheritdoc />
    public Task AssignmentChanged(
        string gameId,
        Guid playerId,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(gameId).SendAsync(nameof(GameHub.AssignmentChanged), playerId, cancellationToken);

    /// <inheritdoc />
    public Task PlayerLeft(
        string gameId,
        Guid playerId,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(gameId).SendAsync(nameof(GameHub.PlayerLeft), playerId, cancellationToken);
}
