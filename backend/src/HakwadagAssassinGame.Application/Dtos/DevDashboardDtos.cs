using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Application.Dtos;

/// <summary>Summary of a game for the dev dashboard.</summary>
public record DevGameSummaryDto(
    Guid Id,
    string Name,
    string InviteCode,
    GameStatus Status,
    int PlayerCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledEndAt);

/// <summary>Player membership in a game for the dev dashboard.</summary>
public record DevPlayerInGameDto(
    Guid PlayerId,
    string Email,
    string DisplayName,
    GameRole Role,
    int Score,
    bool IsActive,
    bool IsParticipating);

/// <summary>Assignment in a game for the dev dashboard.</summary>
public record DevAssignmentDto(
    Guid Id,
    Guid HunterId,
    string HunterName,
    Guid TargetId,
    string TargetName,
    AssignmentStatus Status,
    DateTimeOffset AssignedAt);

/// <summary>Tag submission in a game for the dev dashboard.</summary>
public record DevTagSubmissionDto(
    Guid Id,
    Guid AssignmentId,
    Guid HunterId,
    string HunterName,
    Guid TargetId,
    string TargetName,
    TagStatus Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ResolvedAt);

/// <summary>Request to submit a tag on behalf of a player (dev-only).</summary>
public record DevSubmitTagRequest(
    Guid PlayerId,
    Guid AssignmentId,
    Guid ConditionId);
