using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Application.Dtos;

/// <summary>Describes the current assignment of a hunter.</summary>
public record AssignmentDto(Guid Id, TargetDto Target, List<ConditionDto> Conditions, DateTimeOffset AssignedAt);

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
