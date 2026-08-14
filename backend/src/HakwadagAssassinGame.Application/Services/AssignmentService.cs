using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;

namespace HakwadagAssassinGame.Application.Services;

/// <summary>Provides assignment queries.</summary>
public interface IAssignmentService
{
    /// <summary>Gets the active assignment for a player in a game.</summary>
    Task<AssignmentDto> GetMyAssignmentAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>Gets the time when a player can receive their next assignment in a game.</summary>
    Task<NextAssignmentAvailabilityDto> GetNextAvailabilityAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default);
}

/// <summary>Default assignment query service.</summary>
public sealed class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository assignmentRepository;
    private readonly IPlayerRepository playerRepository;
    private readonly IGamePlayerRepository gamePlayerRepository;
    private readonly IGameRepository gameRepository;

    /// <summary>Initializes the assignment service.</summary>
    public AssignmentService(
        IAssignmentRepository assignmentRepository,
        IPlayerRepository playerRepository,
        IGamePlayerRepository gamePlayerRepository,
        IGameRepository gameRepository)
    {
        this.assignmentRepository = assignmentRepository;
        this.playerRepository = playerRepository;
        this.gamePlayerRepository = gamePlayerRepository;
        this.gameRepository = gameRepository;
    }

    /// <inheritdoc />
    public async Task<AssignmentDto> GetMyAssignmentAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default)
    {
        await ServiceHelpers.RequireMembershipAsync(gamePlayerRepository, gameId, playerId, cancellationToken);
        var assignment = await assignmentRepository.GetActiveByHunterIdAsync(gameId, playerId, cancellationToken)
            ?? throw new AssignmentNotFoundException(playerId);
        if (assignment.Status != AssignmentStatus.Active)
        {
            throw new AssignmentNotFoundException(assignment.Id);
        }

        var target = await ServiceHelpers.RequirePlayerAsync(playerRepository, assignment.TargetId, cancellationToken);
        var memberships = await gamePlayerRepository.GetByGameIdAsync(gameId, cancellationToken);
        var players = new List<Core.Entities.Player>();
        foreach (var membership in memberships.Where(item => item.IsActive))
        {
            players.Add(await ServiceHelpers.RequirePlayerAsync(playerRepository, membership.PlayerId, cancellationToken));
        }

        return ServiceHelpers.MapAssignment(assignment, target, players);
    }

    /// <inheritdoc />
    public async Task<NextAssignmentAvailabilityDto> GetNextAvailabilityAsync(
        Guid playerId,
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        await ServiceHelpers.RequireMembershipAsync(gamePlayerRepository, gameId, playerId, cancellationToken);
        var game = await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);

        if (await assignmentRepository.GetActiveByHunterIdAsync(gameId, playerId, cancellationToken) is not null)
        {
            // The player already has an active assignment.
            return new NextAssignmentAvailabilityDto(null);
        }

        var latest = await assignmentRepository.GetMostRecentByHunterIdAsync(gameId, playerId, cancellationToken);
        if (latest is null || game.AssignmentCooldownMinutes <= 0)
        {
            // No assignment history or no cooldown configured — available right away.
            return new NextAssignmentAvailabilityDto(DateTimeOffset.UtcNow);
        }

        var availableAt = latest.AssignedAt + TimeSpan.FromMinutes(game.AssignmentCooldownMinutes);
        return new NextAssignmentAvailabilityDto(
            availableAt > DateTimeOffset.UtcNow ? availableAt : DateTimeOffset.UtcNow);
    }
}
