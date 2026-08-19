using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Application.Dtos;

/// <summary>Creates a new game. Duration is optional; null means the game has no fixed end time.</summary>
public record CreateGameRequest(
    string Name,
    int? DurationHours,
    int? MaxPlayers,
    int BasePointsPerTag,
    int ConfirmationTimeoutMinutes,
    Dictionary<ConditionType, int>? ConditionBonuses,
    List<SafeTimeBlockDto>? SafeTimeBlocks,
    int AssignmentCooldownMinutes = 30);

/// <summary>Describes a recurring safe-time period.</summary>
public record SafeTimeBlockDto(Guid Id, DateTimeOffset StartTime, DateTimeOffset EndTime);

/// <summary>Represents a game visible to a player.</summary>
public record GameDto(
    Guid Id,
    string Name,
    string InviteCode,
    GameStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledEndAt,
    DateTimeOffset? EndedAt,
    int MaxPlayers,
    int BasePointsPerTag,
    TimeSpan ConfirmationTimeout,
    int AssignmentCooldownMinutes,
    int PlayerCount,
    int ParticipantCount,
    bool IsParticipating,
    GameRole MyRole,
    List<SafeTimeBlockDto> SafeTimeBlocks);

/// <summary>Represents a player in a game with their role.</summary>
public record GamePlayerDto(Guid PlayerId, string DisplayName, string Email, string? AvatarUrl, GameRole Role);

/// <summary>Supplies the display name used when joining a game.</summary>
public record JoinGameRequest(string DisplayName);

/// <summary>Sets whether an admin participates in the game.</summary>
public record SetParticipationRequest(bool IsParticipating);
