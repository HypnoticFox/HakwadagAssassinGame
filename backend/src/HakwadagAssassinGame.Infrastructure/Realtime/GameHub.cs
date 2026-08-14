using Microsoft.AspNetCore.SignalR;

namespace HakwadagAssassinGame.Infrastructure.Realtime;

/// <summary>SignalR hub for real-time game updates.</summary>
public sealed class GameHub : Hub
{
    /// <summary>Adds the current connection to a game group.</summary>
    public async Task JoinGame(string gameId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
    }

    /// <summary>Removes the current connection from a game group.</summary>
    public async Task LeaveGame(string gameId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
    }
}
