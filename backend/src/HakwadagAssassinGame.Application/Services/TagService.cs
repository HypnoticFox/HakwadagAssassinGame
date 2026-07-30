using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;

namespace HakwadagAssassinGame.Application.Services;

/// <summary>Coordinates tag submission and resolution.</summary>
public interface ITagService
{
    /// <summary>Submits a tag for the current assignment.</summary>
    Task<TagSubmissionDto> SubmitTagAsync(Guid playerId, SubmitTagRequest request, CancellationToken cancellationToken = default);

    /// <summary>Confirms a pending tag.</summary>
    Task<TagSubmissionDto> ConfirmTagAsync(Guid playerId, Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>Denies a pending tag.</summary>
    Task<TagSubmissionDto> DenyTagAsync(Guid playerId, Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>Voids a tag as an administrator.</summary>
    Task<TagSubmissionDto> VoidTagAsync(Guid playerId, Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>Gets the pending tag for a target in a game.</summary>
    Task<TagSubmissionDto?> GetPendingTagAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default);
}

/// <summary>Default tag orchestration service.</summary>
public sealed class TagService : ITagService
{
    private readonly ITagSubmissionRepository tagRepository;
    private readonly IAssignmentRepository assignmentRepository;
    private readonly IGameRepository gameRepository;
    private readonly IGamePlayerRepository gamePlayerRepository;
    private readonly IPlayerRepository playerRepository;
    private readonly IPushNotificationService pushNotificationService;
    private readonly IConditionLibrary conditionLibrary;

    /// <summary>Initializes the tag service.</summary>
    public TagService(
        ITagSubmissionRepository tagRepository,
        IAssignmentRepository assignmentRepository,
        IGameRepository gameRepository,
        IGamePlayerRepository gamePlayerRepository,
        IPlayerRepository playerRepository,
        IPushNotificationService pushNotificationService,
        IConditionLibrary conditionLibrary)
    {
        this.tagRepository = tagRepository;
        this.assignmentRepository = assignmentRepository;
        this.gameRepository = gameRepository;
        this.gamePlayerRepository = gamePlayerRepository;
        this.playerRepository = playerRepository;
        this.pushNotificationService = pushNotificationService;
        this.conditionLibrary = conditionLibrary;
    }

    /// <inheritdoc />
    public async Task<TagSubmissionDto> SubmitTagAsync(Guid playerId, SubmitTagRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var assignment = await assignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken)
            ?? throw new AssignmentNotFoundException(request.AssignmentId);
        if (assignment.HunterId != playerId || assignment.Status != AssignmentStatus.Active)
        {
            throw new UnauthorizedException("Only the active hunter can submit this assignment.");
        }

        var game = await ServiceHelpers.RequireGameAsync(gameRepository, assignment.GameId, cancellationToken);
        if (game.Status != GameStatus.Active)
        {
            throw new InvalidGameStateException("Tags can only be submitted while the game is active.");
        }

        if (assignment.Conditions.All(condition => condition.Id != request.ConditionId))
        {
            throw new InvalidGameStateException("The selected condition does not belong to the assignment.");
        }

        if (game.SafeTimeBlocks.Any(block => block.Contains(DateTimeOffset.UtcNow)))
        {
            throw new SafeTimeBlockViolationException();
        }

        var pending = await tagRepository.GetPendingByTargetIdAsync(assignment.TargetId, cancellationToken);
        if (pending.Any(submission => submission.Status == TagStatus.Pending))
        {
            throw new PendingTagExistsException(assignment.TargetId);
        }

        var submission = TagSubmission.Create(
            assignment.Id,
            assignment.HunterId,
            assignment.TargetId,
            request.ConditionId);
        await tagRepository.AddAsync(submission, cancellationToken);
        await pushNotificationService.SendNotificationAsync(
            assignment.TargetId,
            "New tag to confirm",
            "A player has submitted a tag for you to confirm.",
            cancellationToken);
        return ServiceHelpers.MapTag(submission);
    }

    /// <inheritdoc />
    public async Task<TagSubmissionDto> ConfirmTagAsync(Guid playerId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var submission = await RequirePendingAsync(tagId, cancellationToken);
        if (submission.TargetId != playerId)
        {
            throw new UnauthorizedException("Only the target can confirm a tag.");
        }

        var assignment = await assignmentRepository.GetByIdAsync(submission.AssignmentId, cancellationToken)
            ?? throw new AssignmentNotFoundException(submission.AssignmentId);
        var game = await ServiceHelpers.RequireGameAsync(gameRepository, assignment.GameId, cancellationToken);
        submission.Confirm();
        assignment.Complete();
        await tagRepository.UpdateAsync(submission, cancellationToken);
        await assignmentRepository.UpdateAsync(assignment, cancellationToken);

        var hunterMembership = await ServiceHelpers.RequireMembershipAsync(
            gamePlayerRepository, game.Id, submission.HunterId, cancellationToken);
        var condition = assignment.Conditions.First(item => item.Id == submission.ConditionId);
        hunterMembership.AddScore(game.BasePointsPerTag + game.ConditionBonuses.GetValueOrDefault(condition.Type));
        await gamePlayerRepository.UpdateAsync(hunterMembership, cancellationToken);
        await CreateReplacementAsync(game, submission.HunterId, cancellationToken);

        await pushNotificationService.SendNotificationAsync(
            submission.HunterId,
            "Tag confirmed",
            "Your tag was confirmed.",
            cancellationToken);
        return ServiceHelpers.MapTag(submission);
    }

    /// <inheritdoc />
    public async Task<TagSubmissionDto> DenyTagAsync(Guid playerId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var submission = await RequirePendingAsync(tagId, cancellationToken);
        if (submission.TargetId != playerId)
        {
            throw new UnauthorizedException("Only the target can deny a tag.");
        }

        submission.Deny();
        await tagRepository.UpdateAsync(submission, cancellationToken);
        await pushNotificationService.SendNotificationAsync(
            submission.HunterId,
            "Tag denied",
            "Your tag was denied.",
            cancellationToken);
        return ServiceHelpers.MapTag(submission);
    }

    /// <inheritdoc />
    public async Task<TagSubmissionDto> VoidTagAsync(Guid playerId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var submission = await tagRepository.GetByIdAsync(tagId, cancellationToken)
            ?? throw new TagSubmissionNotFoundException(tagId);
        var assignment = await assignmentRepository.GetByIdAsync(submission.AssignmentId, cancellationToken)
            ?? throw new AssignmentNotFoundException(submission.AssignmentId);
        var membership = await ServiceHelpers.RequireMembershipAsync(
            gamePlayerRepository, assignment.GameId, playerId, cancellationToken);
        if (!ServiceHelpers.IsAdmin(membership))
        {
            throw new UnauthorizedException("Only a game administrator can void a tag.");
        }

        if (submission.Status == TagStatus.Confirmed)
        {
            var game = await ServiceHelpers.RequireGameAsync(gameRepository, assignment.GameId, cancellationToken);
            var hunter = await ServiceHelpers.RequireMembershipAsync(
                gamePlayerRepository, assignment.GameId, submission.HunterId, cancellationToken);
            var condition = assignment.Conditions.FirstOrDefault(item => item.Id == submission.ConditionId);
            var points = game.BasePointsPerTag + (condition is null ? 0 : game.ConditionBonuses.GetValueOrDefault(condition.Type));
            hunter.RemoveScore(points);
            await gamePlayerRepository.UpdateAsync(hunter, cancellationToken);
        }

        if (submission.Status == TagStatus.Pending)
        {
            submission.Void();
            await tagRepository.UpdateAsync(submission, cancellationToken);
        }
        else if (submission.Status == TagStatus.Confirmed)
        {
            submission.Void();
            await tagRepository.UpdateAsync(submission, cancellationToken);
        }
        else
        {
            throw new InvalidGameStateException("Only pending or confirmed tags can be voided.");
        }

        return ServiceHelpers.MapTag(submission);
    }

    /// <inheritdoc />
    public async Task<TagSubmissionDto?> GetPendingTagAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default)
    {
        await ServiceHelpers.RequireMembershipAsync(gamePlayerRepository, gameId, playerId, cancellationToken);
        var submissions = await tagRepository.GetPendingByTargetIdAsync(playerId, cancellationToken);
        foreach (var submission in submissions.Where(item => item.Status == TagStatus.Pending))
        {
            var assignment = await assignmentRepository.GetByIdAsync(submission.AssignmentId, cancellationToken);
            if (assignment?.GameId == gameId)
            {
                return ServiceHelpers.MapTag(submission);
            }
        }

        return null;
    }

    private async Task<TagSubmission> RequirePendingAsync(Guid tagId, CancellationToken cancellationToken)
    {
        var submission = await tagRepository.GetByIdAsync(tagId, cancellationToken)
            ?? throw new TagSubmissionNotFoundException(tagId);
        if (submission.Status != TagStatus.Pending)
        {
            throw new InvalidGameStateException("Only a pending tag can be resolved.");
        }
        return submission;
    }

    private async Task CreateReplacementAsync(Game game, Guid hunterId, CancellationToken cancellationToken)
    {
        if (game.Status != GameStatus.Active)
        {
            return;
        }

        var memberships = (await gamePlayerRepository.GetByGameIdAsync(game.Id, cancellationToken))
            .Where(item => item.IsActive).ToList();
        if (memberships.Count < 3)
        {
            return;
        }

        var index = memberships.FindIndex(item => item.PlayerId == hunterId);
        if (index < 0)
        {
            return;
        }

        var targets = ServiceHelpers.CreateDerangement(memberships);
        var players = new List<Player>(memberships.Count);
        foreach (var membership in memberships)
        {
            players.Add(await ServiceHelpers.RequirePlayerAsync(playerRepository, membership.PlayerId, cancellationToken));
        }

        var conditions = await ServiceHelpers.CreateConditions(
            game.Id,
            hunterId,
            players,
            conditionLibrary,
            cancellationToken);
        var replacement = Assignment.Create(
            game.Id,
            hunterId,
            targets[index],
            conditions);
        await assignmentRepository.AddAsync(replacement, cancellationToken);
    }
}
