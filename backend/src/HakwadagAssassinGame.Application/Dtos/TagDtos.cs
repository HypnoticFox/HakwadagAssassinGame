using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Application.Dtos;

/// <summary>Submits a fulfilled assignment condition.</summary>
public record SubmitTagRequest(Guid AssignmentId, Guid ConditionId);

/// <summary>Describes a tag submission.</summary>
public record TagSubmissionDto(
    Guid Id,
    Guid AssignmentId,
    Guid HunterId,
    Guid TargetId,
    Guid ConditionId,
    TagStatus Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ResolvedAt);
