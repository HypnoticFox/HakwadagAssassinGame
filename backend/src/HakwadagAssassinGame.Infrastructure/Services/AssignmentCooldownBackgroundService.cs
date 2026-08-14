using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HakwadagAssassinGame.Infrastructure.Services;

/// <summary>
/// Periodically creates replacement assignments for hunters whose previous assignment was
/// confirmed or denied and whose assignment cooldown has now elapsed.
/// </summary>
public sealed class AssignmentCooldownBackgroundService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<AssignmentCooldownBackgroundService> logger;

    /// <summary>Initializes the assignment cooldown background service.</summary>
    public AssignmentCooldownBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AssignmentCooldownBackgroundService> logger)
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
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Assignment cooldown sweep failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        var gameRepository = provider.GetRequiredService<IGameRepository>();
        var assignmentRepository = provider.GetRequiredService<IAssignmentRepository>();
        var gamePlayerRepository = provider.GetRequiredService<IGamePlayerRepository>();
        var tagService = provider.GetRequiredService<ITagService>();

        var games = await gameRepository.GetAllAsync(cancellationToken);
        foreach (var game in games.Where(item => item.Status == GameStatus.Active))
        {
            var assignments = await assignmentRepository.GetByGameIdAsync(game.Id, cancellationToken);
            var memberships = await gamePlayerRepository.GetByGameIdAsync(game.Id, cancellationToken);
            var participating = memberships
                .Where(item => item.IsActive && item.IsParticipating)
                .Select(item => item.PlayerId)
                .ToHashSet();

            var candidates = assignments
                .Where(item => item.Status == AssignmentStatus.Completed)
                .Select(item => item.HunterId)
                .Distinct()
                .Where(participating.Contains);

            foreach (var hunterId in candidates)
            {
                await tagService.CreateReplacementIfReadyAsync(game.Id, hunterId, cancellationToken);
            }
        }
    }
}
