using HakwadagAssassinGame.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HakwadagAssassinGame.Infrastructure.Services;

/// <summary>
/// Periodically auto-confirms pending tags that have exceeded their game's confirmation timeout,
/// so unresolved tags do not stall the game indefinitely.
/// </summary>
public sealed class TagTimeoutBackgroundService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TagTimeoutBackgroundService> logger;

    /// <summary>Initializes the tag timeout background service.</summary>
    public TagTimeoutBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TagTimeoutBackgroundService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var tagService = scope.ServiceProvider.GetRequiredService<ITagService>();
                var confirmed = await tagService.AutoConfirmExpiredTagsAsync(stoppingToken);
                if (confirmed > 0)
                {
                    logger.LogInformation("Auto-confirmed {Count} expired tag(s).", confirmed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Tag timeout sweep failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
