using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;

namespace HakwadagAssassinGame.Application.Services;

/// <summary>Coordinates game creation and lifecycle operations.</summary>
public interface IGameService
{
    /// <summary>Creates a game and adds its creator.</summary>
    Task<GameDto> CreateGameAsync(Guid playerId, CreateGameRequest request, CancellationToken cancellationToken = default);

    /// <summary>Adds a player to a game by invite code.</summary>
    Task<GameDto> JoinGameAsync(Guid playerId, string inviteCode, string displayName, CancellationToken cancellationToken = default);

    /// <summary>Adds a player to a game using a join request.</summary>
    Task<GameDto> JoinGameAsync(Guid playerId, string inviteCode, JoinGameRequest request, CancellationToken cancellationToken = default);

    /// <summary>Starts a game and creates its initial assignments.</summary>
    Task<GameDto> StartGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>Ends a game.</summary>
    Task<GameDto> EndGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>Gets a game from the perspective of a player.</summary>
    Task<GameDto> GetGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>Gets all players in a game with their roles.</summary>
    Task<IReadOnlyList<GamePlayerDto>> GetPlayersAsync(Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>Gets all games the player belongs to.</summary>
    Task<IReadOnlyList<GameDto>> GetMyGamesAsync(Guid playerId, CancellationToken cancellationToken = default);

    /// <summary>Leaves a game and repairs affected assignments.</summary>
    Task LeaveGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>Rejoins a game the player previously left during active play.</summary>
    Task<GameDto> RejoinGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default);
}

/// <summary>Default game orchestration service.</summary>
public sealed class GameService : IGameService
{
    private readonly IGameRepository gameRepository;
    private readonly IPlayerRepository playerRepository;
    private readonly IGamePlayerRepository gamePlayerRepository;
    private readonly IAssignmentRepository assignmentRepository;
    private readonly ITagSubmissionRepository tagSubmissionRepository;
    private readonly IInviteCodeGenerator inviteCodeGenerator;
    private readonly IConditionLibrary conditionLibrary;

    /// <summary>Initializes the game service.</summary>
    public GameService(
        IGameRepository gameRepository,
        IPlayerRepository playerRepository,
        IGamePlayerRepository gamePlayerRepository,
        IAssignmentRepository assignmentRepository,
        ITagSubmissionRepository tagSubmissionRepository,
        IInviteCodeGenerator inviteCodeGenerator,
        IConditionLibrary conditionLibrary)
    {
        this.gameRepository = gameRepository;
        this.playerRepository = playerRepository;
        this.gamePlayerRepository = gamePlayerRepository;
        this.assignmentRepository = assignmentRepository;
        this.tagSubmissionRepository = tagSubmissionRepository;
        this.inviteCodeGenerator = inviteCodeGenerator;
        this.conditionLibrary = conditionLibrary;
    }

    /// <inheritdoc />
    public async Task<GameDto> CreateGameAsync(Guid playerId, CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        await ServiceHelpers.RequirePlayerAsync(playerRepository, playerId, cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        if (request.DurationHours <= 0 || request.ConfirmationTimeoutMinutes <= 0)
        {
            throw new InvalidGameStateException("Duration and confirmation timeout must be positive.");
        }

        var safeTimeBlocks = request.SafeTimeBlocks?.Select(block => SafeTimeBlock.Create(block.StartTime, block.EndTime, block.Day));
        var game = Game.Create(
            request.Name,
            inviteCodeGenerator.GenerateCode(),
            DateTimeOffset.UtcNow.AddHours(request.DurationHours),
            request.MaxPlayers ?? 50,
            request.BasePointsPerTag,
            request.ConditionBonuses,
            TimeSpan.FromMinutes(request.ConfirmationTimeoutMinutes),
            safeTimeBlocks);
        await gameRepository.AddAsync(game, cancellationToken);
        await gamePlayerRepository.AddAsync(GamePlayer.Create(game.Id, playerId, GameRole.Creator), cancellationToken);
        await conditionLibrary.GetAsync(game.Id, cancellationToken);
        return await ToDtoAsync(game, playerId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<GameDto> JoinGameAsync(Guid playerId, string inviteCode, JoinGameRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return JoinGameAsync(playerId, inviteCode, request.DisplayName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GameDto> JoinGameAsync(Guid playerId, string inviteCode, string displayName, CancellationToken cancellationToken = default)
    {
        var player = await ServiceHelpers.RequirePlayerAsync(playerRepository, playerId, cancellationToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(inviteCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        var game = await gameRepository.GetByInviteCodeAsync(inviteCode.Trim(), cancellationToken)
            ?? throw new GameNotFoundException(Guid.Empty);
        if (game.Status != GameStatus.NotStarted && game.Status != GameStatus.Active)
        {
            throw new InvalidGameStateException("Players can only join a game before it starts or while it is active.");
        }

        var membership = await gamePlayerRepository.GetAsync(game.Id, playerId, cancellationToken);
        if (membership is { IsActive: true, IsParticipating: true })
        {
            throw new InvalidGameStateException("The player is already in this game.");
        }

        if (membership is { IsActive: false })
        {
            throw new InvalidGameStateException("The player has permanently left this game and cannot rejoin.");
        }

        var memberships = await gamePlayerRepository.GetByGameIdAsync(game.Id, cancellationToken);

        if (membership is null)
        {
            // New player — check capacity
            if (memberships.Count(m => m.IsActive) >= game.MaxPlayers)
            {
                throw new InvalidGameStateException("The game is full.");
            }
        }

        player.ChangeDisplayName(displayName.Trim());
        await playerRepository.UpdateAsync(player, cancellationToken);

        if (membership is { IsActive: true, IsParticipating: false })
        {
            // Rejoining — reset score and re-enable participation
            membership.SetParticipating(true);
            membership.ResetScore();
            await gamePlayerRepository.UpdateAsync(membership, cancellationToken);

            if (game.Status == GameStatus.Active)
            {
                // Gather all participating players (including the rejoining player)
                var allParticipating = memberships
                    .Where(m => m.IsActive && m.IsParticipating && m.PlayerId != playerId)
                    .Concat([membership])
                    .ToList();

                if (allParticipating.Count >= 3)
                {
                    var players = await LoadPlayersAsync(allParticipating, cancellationToken);
                    var targets = ServiceHelpers.CreateDerangement(allParticipating);

                    // Create assignment for the rejoining player
                    var rejoinIndex = allParticipating.FindIndex(m => m.PlayerId == playerId);
                    if (rejoinIndex >= 0)
                    {
                        var conditions = await ServiceHelpers.CreateConditions(
                            game.Id, playerId, players, conditionLibrary, cancellationToken);
                        var assignment = Assignment.Create(
                            game.Id, playerId, targets[rejoinIndex], conditions);
                        await assignmentRepository.AddAsync(assignment, cancellationToken);
                    }

                    // Repair assignments for players whose target was the rejoining player
                    var activeAssignments = await assignmentRepository.GetByGameIdAsync(game.Id, cancellationToken);
                    foreach (var affected in activeAssignments.Where(a =>
                        a.Status == AssignmentStatus.TargetLeft && a.TargetId == playerId))
                    {
                        var hunterIndex = allParticipating.FindIndex(m => m.PlayerId == affected.HunterId);
                        if (hunterIndex < 0)
                        {
                            continue;
                        }

                        var replacementConditions = await ServiceHelpers.CreateConditions(
                            game.Id, affected.HunterId, players, conditionLibrary, cancellationToken);
                        var replacement = Assignment.Create(
                            game.Id, affected.HunterId, targets[hunterIndex], replacementConditions);
                        await assignmentRepository.AddAsync(replacement, cancellationToken);
                    }
                }
            }
        }
        else
        {
            // New player
            await gamePlayerRepository.AddAsync(GamePlayer.Create(game.Id, playerId), cancellationToken);
        }

        return await ToDtoAsync(game, playerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GameDto> StartGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default)
    {
        var game = await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        var creator = await ServiceHelpers.RequireMembershipAsync(gamePlayerRepository, gameId, playerId, cancellationToken);
        if (!ServiceHelpers.IsAdmin(creator))
        {
            throw new UnauthorizedException("Only a game administrator can start the game.");
        }

        var memberships = (await gamePlayerRepository.GetByGameIdAsync(gameId, cancellationToken))
            .Where(membership => membership.IsActive).ToList();
        var participants = memberships.Where(m => m.IsParticipating).ToList();
        if (participants.Count < 3)
        {
            throw new InvalidGameStateException("At least three participating players are required to start a game.");
        }

        try
        {
            game.Start();
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidGameStateException(exception.Message);
        }

        var players = await LoadPlayersAsync(participants, cancellationToken);
        var targets = ServiceHelpers.CreateDerangement(participants);
        for (var index = 0; index < participants.Count; index++)
        {
            var conditions = await ServiceHelpers.CreateConditions(
                gameId,
                participants[index].PlayerId,
                players,
                conditionLibrary,
                cancellationToken);
            var assignment = Assignment.Create(
                gameId,
                participants[index].PlayerId,
                targets[index],
                conditions);
            await assignmentRepository.AddAsync(assignment, cancellationToken);
        }

        await gameRepository.UpdateAsync(game, cancellationToken);
        return await ToDtoAsync(game, playerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GameDto> EndGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default)
    {
        var game = await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        var membership = await ServiceHelpers.RequireMembershipAsync(gamePlayerRepository, gameId, playerId, cancellationToken);
        if (!ServiceHelpers.IsAdmin(membership))
        {
            throw new UnauthorizedException("Only a game administrator can end the game.");
        }

        game.End();
        await gameRepository.UpdateAsync(game, cancellationToken);
        return await ToDtoAsync(game, playerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GameDto> GetGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default)
    {
        var game = await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        await ServiceHelpers.RequireMembershipAsync(gamePlayerRepository, gameId, playerId, cancellationToken);
        return await ToDtoAsync(game, playerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GamePlayerDto>> GetPlayersAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        var memberships = await gamePlayerRepository.GetByGameIdAsync(gameId, cancellationToken);
        var players = new List<GamePlayerDto>(memberships.Count);
        foreach (var membership in memberships)
        {
            var player = await ServiceHelpers.RequirePlayerAsync(playerRepository, membership.PlayerId, cancellationToken);
            players.Add(new GamePlayerDto(
                player.Id,
                player.DisplayName,
                player.Email,
                player.AvatarUrl,
                membership.Role));
        }

        return players
            .OrderBy(player => player.Role switch
            {
                GameRole.Creator => 0,
                GameRole.CoAdmin => 1,
                _ => 2
            })
            .ThenBy(player => player.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameDto>> GetMyGamesAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await gamePlayerRepository.GetByPlayerIdAsync(playerId, cancellationToken);
        var games = new List<GameDto>(memberships.Count);
        foreach (var membership in memberships)
        {
            var game = await gameRepository.GetByIdAsync(membership.GameId, cancellationToken);
            if (game is not null)
            {
                games.Add(await ToDtoAsync(game, playerId, cancellationToken));
            }
        }

        return games;
    }

    /// <inheritdoc />
    public async Task LeaveGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default)
    {
        var game = await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        var membership = await ServiceHelpers.RequireMembershipAsync(gamePlayerRepository, gameId, playerId, cancellationToken);
        if (!membership.IsActive)
        {
            throw new InvalidGameStateException("The player has already left this game.");
        }

        var memberships = (await gamePlayerRepository.GetByGameIdAsync(gameId, cancellationToken))
            .Where(item => item.IsActive).ToList();
        var assignments = await assignmentRepository.GetByGameIdAsync(gameId, cancellationToken);
        if (game.Status == GameStatus.Active)
        {
            membership.SetParticipating(false);
        }
        else
        {
            membership.Deactivate();
        }

        await gamePlayerRepository.UpdateAsync(membership, cancellationToken);

        foreach (var assignment in assignments.Where(item => item.Status == AssignmentStatus.Active &&
                     (item.HunterId == playerId || item.TargetId == playerId)))
        {
            if (assignment.TargetId == playerId)
            {
                assignment.MarkTargetLeft();
            }
            else
            {
                assignment.Void();
            }
            await assignmentRepository.UpdateAsync(assignment, cancellationToken);
        }

        foreach (var otherMembership in memberships)
        {
            var pending = await tagSubmissionRepository.GetPendingByTargetIdAsync(otherMembership.PlayerId, cancellationToken);
            foreach (var tag in pending.Where(tag => tag.HunterId == playerId || tag.TargetId == playerId))
            {
                tag.Void();
                await tagSubmissionRepository.UpdateAsync(tag, cancellationToken);
            }
        }

        var remaining = memberships.Where(item => item.PlayerId != playerId && item.IsParticipating).ToList();
        if (game.Status == GameStatus.Active && remaining.Count >= 3)
        {
            var players = await LoadPlayersAsync(remaining, cancellationToken);
            var targets = ServiceHelpers.CreateDerangement(remaining);
            foreach (var affected in assignments.Where(item => item.Status == AssignmentStatus.TargetLeft && item.TargetId == playerId))
            {
                var hunterIndex = remaining.FindIndex(item => item.PlayerId == affected.HunterId);
                if (hunterIndex < 0)
                {
                    continue;
                }

                var conditions = await ServiceHelpers.CreateConditions(
                    gameId,
                    affected.HunterId,
                    players,
                    conditionLibrary,
                    cancellationToken);
                var replacement = Assignment.Create(
                    gameId,
                    affected.HunterId,
                    targets[hunterIndex],
                    conditions);
                await assignmentRepository.AddAsync(replacement, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task<GameDto> RejoinGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default)
    {
        var game = await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        var membership = await ServiceHelpers.RequireMembershipAsync(gamePlayerRepository, gameId, playerId, cancellationToken);

        if (!membership.IsActive)
        {
            throw new InvalidGameStateException("The player has permanently left this game and cannot rejoin.");
        }

        if (membership.IsParticipating)
        {
            throw new InvalidGameStateException("The player is already participating in this game.");
        }

        if (game.Status != GameStatus.Active)
        {
            throw new InvalidGameStateException("Players can only rejoin during an active game.");
        }

        // Re-enable participation and reset score
        membership.SetParticipating(true);
        membership.ResetScore();
        await gamePlayerRepository.UpdateAsync(membership, cancellationToken);

        // Gather all participating players (including the rejoining player)
        var memberships = await gamePlayerRepository.GetByGameIdAsync(gameId, cancellationToken);
        var allParticipating = memberships
            .Where(m => m.IsActive && m.IsParticipating)
            .ToList();

        if (allParticipating.Count >= 3)
        {
            var players = await LoadPlayersAsync(allParticipating, cancellationToken);
            var targets = ServiceHelpers.CreateDerangement(allParticipating);

            // Create assignment for the rejoining player
            var rejoinIndex = allParticipating.FindIndex(m => m.PlayerId == playerId);
            if (rejoinIndex >= 0)
            {
                var conditions = await ServiceHelpers.CreateConditions(
                    game.Id, playerId, players, conditionLibrary, cancellationToken);
                var assignment = Assignment.Create(
                    game.Id, playerId, targets[rejoinIndex], conditions);
                await assignmentRepository.AddAsync(assignment, cancellationToken);
            }

            // Repair assignments for players whose target was the rejoining player
            var activeAssignments = await assignmentRepository.GetByGameIdAsync(gameId, cancellationToken);
            foreach (var affected in activeAssignments.Where(a =>
                a.Status == AssignmentStatus.TargetLeft && a.TargetId == playerId))
            {
                var hunterIndex = allParticipating.FindIndex(m => m.PlayerId == affected.HunterId);
                if (hunterIndex < 0)
                {
                    continue;
                }

                var replacementConditions = await ServiceHelpers.CreateConditions(
                    game.Id, affected.HunterId, players, conditionLibrary, cancellationToken);
                var replacement = Assignment.Create(
                    game.Id, affected.HunterId, targets[hunterIndex], replacementConditions);
                await assignmentRepository.AddAsync(replacement, cancellationToken);
            }
        }

        return await ToDtoAsync(game, playerId, cancellationToken);
    }

    private async Task<GameDto> ToDtoAsync(Game game, Guid playerId, CancellationToken cancellationToken)
    {
        var memberships = await gamePlayerRepository.GetByGameIdAsync(game.Id, cancellationToken);
        var membership = memberships.FirstOrDefault(item => item.PlayerId == playerId)
            ?? throw new UnauthorizedException("The player is not a member of this game.");
        return new GameDto(
            game.Id,
            game.Name,
            game.InviteCode,
            game.Status,
            game.CreatedAt,
            game.ScheduledEndAt,
            game.EndedAt,
            game.MaxPlayers,
            game.BasePointsPerTag,
            game.ConfirmationTimeout,
            memberships.Count(item => item.IsActive),
            memberships.Count(m => m.IsActive && m.IsParticipating),
            membership.IsParticipating,
            membership.Role,
            game.SafeTimeBlocks.Select(block => new SafeTimeBlockDto(block.Id, block.StartTime, block.EndTime, block.Day)).ToList());
    }

    private async Task<List<Player>> LoadPlayersAsync(IReadOnlyList<GamePlayer> memberships, CancellationToken cancellationToken)
    {
        var players = new List<Player>(memberships.Count);
        foreach (var membership in memberships)
        {
            players.Add(await ServiceHelpers.RequirePlayerAsync(playerRepository, membership.PlayerId, cancellationToken));
        }
        return players;
    }
}
