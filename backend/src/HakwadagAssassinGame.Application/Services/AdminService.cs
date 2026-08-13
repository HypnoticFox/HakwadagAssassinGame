using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;

namespace HakwadagAssassinGame.Application.Services;

/// <summary>Coordinates game administration operations.</summary>
public interface IAdminService
{
    /// <summary>Promotes a player to co-admin.</summary>
    Task AddCoAdminAsync(Guid creatorId, Guid gameId, Guid targetPlayerId, CancellationToken cancellationToken = default);

    /// <summary>Demotes a co-admin.</summary>
    Task RemoveCoAdminAsync(Guid creatorId, Guid gameId, Guid targetPlayerId, CancellationToken cancellationToken = default);

    /// <summary>Adds a safe-time block and returns its identifier.</summary>
    Task<Guid> AddSafeTimeBlockAsync(Guid playerId, Guid gameId, AddSafeTimeBlockRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a safe-time block.</summary>
    Task RemoveSafeTimeBlockAsync(Guid playerId, Guid gameId, Guid blockId, CancellationToken cancellationToken = default);

    /// <summary>Adds a custom condition to a game's condition library.</summary>
    Task AddCustomConditionAsync(Guid playerId, Guid gameId, AddCustomConditionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sets whether an admin participates in the game.</summary>
    Task SetParticipationAsync(Guid playerId, Guid gameId, bool isParticipating, CancellationToken cancellationToken = default);

    /// <summary>Updates the scheduled duration before the game starts.</summary>
    Task UpdateDurationAsync(Guid playerId, Guid gameId, UpdateDurationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Extends the remaining time of an active game.</summary>
    Task ExtendDurationAsync(Guid playerId, Guid gameId, ExtendDurationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Default game administration service.</summary>
public sealed class AdminService : IAdminService
{
    private readonly IGameRepository gameRepository;
    private readonly IGamePlayerRepository gamePlayerRepository;
    private readonly IConditionLibrary conditionLibrary;

    /// <summary>Initializes the administration service.</summary>
    public AdminService(
        IGameRepository gameRepository,
        IGamePlayerRepository gamePlayerRepository,
        IConditionLibrary conditionLibrary)
    {
        this.gameRepository = gameRepository;
        this.gamePlayerRepository = gamePlayerRepository;
        this.conditionLibrary = conditionLibrary;
    }

    /// <inheritdoc />
    public async Task AddCoAdminAsync(Guid creatorId, Guid gameId, Guid targetPlayerId, CancellationToken cancellationToken = default)
    {
        await RequireCreatorAsync(creatorId, gameId, cancellationToken);
        var target = await ServiceHelpers.RequireMembershipAsync(gamePlayerRepository, gameId, targetPlayerId, cancellationToken);
        if (!target.IsActive)
        {
            throw new InvalidGameStateException("An inactive player cannot become an administrator.");
        }

        target.PromoteToCoAdmin();
        await gamePlayerRepository.UpdateAsync(target, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveCoAdminAsync(Guid creatorId, Guid gameId, Guid targetPlayerId, CancellationToken cancellationToken = default)
    {
        await RequireCreatorAsync(creatorId, gameId, cancellationToken);
        var target = await ServiceHelpers.RequireMembershipAsync(gamePlayerRepository, gameId, targetPlayerId, cancellationToken);
        target.DemoteToPlayer();
        await gamePlayerRepository.UpdateAsync(target, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid> AddSafeTimeBlockAsync(Guid playerId, Guid gameId, AddSafeTimeBlockRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await RequireAdminAsync(playerId, gameId, cancellationToken);
        var game = await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        var block = SafeTimeBlock.Create(request.StartTime, request.EndTime, request.Day);
        game.SafeTimeBlocks.Add(block);
        await gameRepository.UpdateAsync(game, cancellationToken);
        return block.Id;
    }

    /// <inheritdoc />
    public async Task RemoveSafeTimeBlockAsync(Guid playerId, Guid gameId, Guid blockId, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(playerId, gameId, cancellationToken);
        var game = await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        var block = game.SafeTimeBlocks.FirstOrDefault(item => item.Id == blockId)
            ?? throw new InvalidGameStateException("The safe-time block was not found.");
        if (!game.SafeTimeBlocks.Remove(block))
        {
            throw new InvalidGameStateException("The safe-time block was not found.");
        }

        await gameRepository.UpdateAsync(game, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddCustomConditionAsync(Guid playerId, Guid gameId, AddCustomConditionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await RequireAdminAsync(playerId, gameId, cancellationToken);
        await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Description);
        await conditionLibrary.AddAsync(gameId, CustomCondition.Create(request.Description), cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetParticipationAsync(Guid playerId, Guid gameId, bool isParticipating, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(playerId, gameId, cancellationToken);
        var game = await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        var membership = await ServiceHelpers.RequireMembershipAsync(gamePlayerRepository, gameId, playerId, cancellationToken);
        if (game.Status != GameStatus.NotStarted)
        {
            throw new InvalidGameStateException("Participation can only be changed before the game starts.");
        }

        membership.SetParticipating(isParticipating);
        await gamePlayerRepository.UpdateAsync(membership, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateDurationAsync(Guid playerId, Guid gameId, UpdateDurationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await RequireCreatorAsync(playerId, gameId, cancellationToken);
        var game = await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        if (request.DurationHours <= 0)
        {
            throw new InvalidGameStateException("Duration must be positive.");
        }
        try
        {
            game.SetScheduledEnd(DateTimeOffset.UtcNow.AddHours(request.DurationHours));
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidGameStateException(exception.Message);
        }
        await gameRepository.UpdateAsync(game, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ExtendDurationAsync(Guid playerId, Guid gameId, ExtendDurationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await RequireCreatorAsync(playerId, gameId, cancellationToken);
        var game = await ServiceHelpers.RequireGameAsync(gameRepository, gameId, cancellationToken);
        if (request.Minutes <= 0)
        {
            throw new InvalidGameStateException("Extension must be positive.");
        }
        try
        {
            game.ExtendTime(TimeSpan.FromMinutes(request.Minutes));
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidGameStateException(exception.Message);
        }
        await gameRepository.UpdateAsync(game, cancellationToken);
    }

    private async Task<GamePlayer> RequireAdminAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken)
    {
        var membership = await ServiceHelpers.RequireMembershipAsync(gamePlayerRepository, gameId, playerId, cancellationToken);
        if (!ServiceHelpers.IsAdmin(membership))
        {
            throw new UnauthorizedException("Only a game administrator can perform this operation.");
        }

        return membership;
    }

    private async Task<GamePlayer> RequireCreatorAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken)
    {
        var membership = await ServiceHelpers.RequireMembershipAsync(gamePlayerRepository, gameId, playerId, cancellationToken);
        if (membership.Role != Core.Enums.GameRole.Creator)
        {
            throw new UnauthorizedException("Only the creator can manage co-admins.");
        }

        return membership;
    }
}
