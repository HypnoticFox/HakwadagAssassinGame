using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;

namespace HakwadagAssassinGame.Application.Services;

/// <summary>Provides game scoreboards.</summary>
public interface ILeaderboardService
{
    /// <summary>Gets players ordered by score and confirmed tags.</summary>
    Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(Guid gameId, CancellationToken cancellationToken = default);
}

/// <summary>Default leaderboard query service.</summary>
public sealed class LeaderboardService : ILeaderboardService
{
    private readonly IGameRepository gameRepository;
    private readonly IGamePlayerRepository gamePlayerRepository;
    private readonly IPlayerRepository playerRepository;
    private readonly IAssignmentRepository assignmentRepository;

    /// <summary>Initializes the leaderboard service.</summary>
    public LeaderboardService(
        IGameRepository gameRepository,
        IGamePlayerRepository gamePlayerRepository,
        IPlayerRepository playerRepository,
        IAssignmentRepository assignmentRepository)
    {
        this.gameRepository = gameRepository;
        this.gamePlayerRepository = gamePlayerRepository;
        this.playerRepository = playerRepository;
        this.assignmentRepository = assignmentRepository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        var memberships = await gamePlayerRepository.GetByGameIdAsync(gameId, cancellationToken);
        var assignments = await assignmentRepository.GetByGameIdAsync(gameId, cancellationToken);
        var entries = new List<LeaderboardEntryDto>(memberships.Count);
        foreach (var membership in memberships)
        {
            var player = await ServiceHelpers.RequirePlayerAsync(playerRepository, membership.PlayerId, cancellationToken);
            var tags = assignments.Count(assignment => assignment.HunterId == player.Id && assignment.Status == AssignmentStatus.Completed);
            entries.Add(new LeaderboardEntryDto(PlayerDto.FromEntity(player), membership.Score, tags));
        }

        return entries
            .OrderByDescending(entry => entry.Score)
            .ThenByDescending(entry => entry.Tags)
            .ThenBy(entry => entry.Player.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
