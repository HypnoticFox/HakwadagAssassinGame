using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Application.Dtos;

/// <summary>Creates a new game.</summary>
public record CreateGameRequest(
    string Name,
    int DurationHours,
    int? MaxPlayers,
    int BasePointsPerTag,
    int ConfirmationTimeoutMinutes,
    Dictionary<ConditionType, int>? ConditionBonuses,
    List<SafeTimeBlockDto>? SafeTimeBlocks);

/// <summary>Describes a recurring safe-time period.</summary>
public record SafeTimeBlockDto(Guid Id, TimeSpan StartTime, TimeSpan EndTime, DayOfWeek? Day);

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
    int PlayerCount,
    GameRole MyRole,
    List<SafeTimeBlockDto> SafeTimeBlocks);

/// <summary>Supplies the display name used when joining a game.</summary>
public record JoinGameRequest(string DisplayName);
