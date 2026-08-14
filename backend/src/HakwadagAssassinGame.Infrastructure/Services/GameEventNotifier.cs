using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Interfaces;
using HakwadagAssassinGame.Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace HakwadagAssassinGame.Infrastructure.Services;

/// <summary>Sends game events to connected clients via SignalR.</summary>
public sealed class GameEventNotifier : IGameEventNotifier
{
    private readonly IHubContext<GameHub> hubContext;

    /// <summary>Initializes the game event notifier.</summary>
    public GameEventNotifier(IHubContext<GameHub> hubContext)
    {
        this.hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task TagResolvedAsync(string gameId, TagSubmissionDto tag, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group(gameId).SendAsync("TagResolved", gameId, tag, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AssignmentChangedAsync(string gameId, string playerId, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group(gameId).SendAsync("AssignmentChanged", gameId, playerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task TagSubmittedAsync(string gameId, TagSubmissionDto tag, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group(gameId).SendAsync("TagSubmitted", gameId, tag, cancellationToken);
    }

    /// <inheritdoc />
    public async Task GameStartedAsync(string gameId, GameDto game, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group(gameId).SendAsync("GameStarted", gameId, game, cancellationToken);
    }

    /// <inheritdoc />
    public async Task GameEndedAsync(string gameId, GameDto game, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group(gameId).SendAsync("GameEnded", gameId, game, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ScoreUpdatedAsync(string gameId, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group(gameId).SendAsync("ScoreUpdated", gameId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PlayerLeftAsync(string gameId, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group(gameId).SendAsync("PlayerLeft", gameId, cancellationToken);
    }
}
