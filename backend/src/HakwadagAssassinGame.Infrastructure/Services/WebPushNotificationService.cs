using HakwadagAssassinGame.Core.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HakwadagAssassinGame.Infrastructure.Services;

/// <summary>Stores Web Push subscriptions and logs notifications until delivery is implemented.</summary>
public sealed class WebPushNotificationService : IPushNotificationService
{
    private readonly IDatabase database;
    private readonly ILogger<WebPushNotificationService> logger;

    /// <summary>Initializes the Web Push notification service.</summary>
    public WebPushNotificationService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<WebPushNotificationService> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        database = connectionMultiplexer.GetDatabase();
    }

    /// <inheritdoc />
    public async Task SendNotificationAsync(
        Guid playerId,
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            "Push notification for player {PlayerId}: {Title} - {Message}",
            playerId,
            title,
            message);
        await Task.CompletedTask;
    }

    /// <summary>Registers a serialized Web Push subscription for a player.</summary>
    public async Task RegisterSubscriptionAsync(
        Guid playerId,
        string subscriptionJson,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionJson);
        cancellationToken.ThrowIfCancellationRequested();
        await database.SetAddAsync($"push:subscriptions:{playerId}", subscriptionJson)
            .WaitAsync(cancellationToken);
    }
}
