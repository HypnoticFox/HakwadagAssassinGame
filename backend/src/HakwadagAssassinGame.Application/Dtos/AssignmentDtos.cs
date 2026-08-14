using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Application.Dtos;

/// <summary>Describes the current assignment of a hunter.</summary>
public record AssignmentDto(Guid Id, TargetDto Target, List<ConditionDto> Conditions, DateTimeOffset AssignedAt);

/// <summary>Describes when a player can receive their next assignment. Null means the player already has an active assignment.</summary>
public record NextAssignmentAvailabilityDto(DateTimeOffset? AvailableAt);

/// <summary>Public target data.</summary>
public record TargetDto(Guid Id, string DisplayName, string? AvatarUrl);

/// <summary>Describes an assignment condition.</summary>
public record ConditionDto(
    Guid Id,
    ConditionType Type,
    string Description,
    string? TargetPersonName,
    string? Action,
    int? MinPeople);
