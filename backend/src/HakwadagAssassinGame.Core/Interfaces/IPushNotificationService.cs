namespace HakwadagAssassinGame.Core.Interfaces;

/// <summary>Delivers push notifications to players.</summary>
public interface IPushNotificationService
{
    /// <summary>Sends a notification to a player.</summary>
    Task SendNotificationAsync(
        Guid playerId,
        string title,
        string message,
        CancellationToken cancellationToken = default);
}
