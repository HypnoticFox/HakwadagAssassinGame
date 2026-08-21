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
    private readonly ITagSubmissionRepository tagRepository;

    /// <summary>Initializes the leaderboard service.</summary>
    public LeaderboardService(
        IGameRepository gameRepository,
        IGamePlayerRepository gamePlayerRepository,
        IPlayerRepository playerRepository,
        IAssignmentRepository assignmentRepository,
        ITagSubmissionRepository tagRepository)
    {
        this.gameRepository = gameRepository;
        this.gamePlayerRepository = gamePlayerRepository;
        this.playerRepository = playerRepository;
        this.assignmentRepository = assignmentRepository;
        this.tagRepository = tagRepository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        var memberships = await gamePlayerRepository.GetByGameIdAsync(gameId, cancellationToken);
        var participatingMemberships = memberships.Where(m => m.IsActive && m.IsParticipating).ToList();
        var assignments = await assignmentRepository.GetByGameIdAsync(gameId, cancellationToken);

        // Count confirmed tags per hunter. Denied or voided tags do not count,
        // even though their assignments are marked Completed.
        var confirmedTagsByHunter = new Dictionary<Guid, int>();
        foreach (var assignment in assignments)
        {
            var submissions = await tagRepository.GetByAssignmentIdAsync(assignment.Id, cancellationToken);
            var confirmedCount = submissions.Count(s => s.Status == TagStatus.Confirmed);
            if (confirmedCount > 0)
            {
                confirmedTagsByHunter.TryGetValue(assignment.HunterId, out var current);
                confirmedTagsByHunter[assignment.HunterId] = current + confirmedCount;
            }
        }

        var entries = new List<LeaderboardEntryDto>(participatingMemberships.Count);
        foreach (var membership in participatingMemberships)
        {
            var player = await ServiceHelpers.RequirePlayerAsync(playerRepository, membership.PlayerId, cancellationToken);
            var tags = confirmedTagsByHunter.GetValueOrDefault(player.Id);
            entries.Add(new LeaderboardEntryDto(PlayerDto.FromEntity(player), membership.Score, tags));
        }

        return entries
            .OrderByDescending(entry => entry.Score)
            .ThenByDescending(entry => entry.Tags)
            .ThenBy(entry => entry.Player.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
