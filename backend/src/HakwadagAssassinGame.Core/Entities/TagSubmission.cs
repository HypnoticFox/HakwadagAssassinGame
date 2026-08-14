using HakwadagAssassinGame.Core.Enums;
using System.Text.Json.Serialization;

namespace HakwadagAssassinGame.Core.Entities;

/// <summary>Represents a hunter's submitted tag awaiting target resolution.</summary>
public sealed class TagSubmission
{
    [JsonConstructor]
    public TagSubmission(
        Guid id,
        Guid assignmentId,
        Guid hunterId,
        Guid targetId,
        Guid conditionId,
        DateTimeOffset submittedAt,
        TagStatus status = TagStatus.Pending,
        DateTimeOffset? resolvedAt = null)
    {
        ValidateIdentifier(assignmentId, nameof(assignmentId));
        ValidateIdentifier(hunterId, nameof(hunterId));
        ValidateIdentifier(targetId, nameof(targetId));
        ValidateIdentifier(conditionId, nameof(conditionId));

        Id = id;
        AssignmentId = assignmentId;
        HunterId = hunterId;
        TargetId = targetId;
        ConditionId = conditionId;
        Status = status;
        SubmittedAt = submittedAt;
        ResolvedAt = resolvedAt;
    }

    /// <summary>Creates a pending tag submission.</summary>
    public static TagSubmission Create(
        Guid assignmentId,
        Guid hunterId,
        Guid targetId,
        Guid conditionId,
        Guid? id = null,
        DateTimeOffset? submittedAt = null) => new(
            id ?? Guid.NewGuid(),
            assignmentId,
            hunterId,
            targetId,
            conditionId,
            submittedAt ?? DateTimeOffset.UtcNow);

    /// <summary>Gets the submission identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the assignment identifier.</summary>
    public Guid AssignmentId { get; private set; }

    /// <summary>Gets the hunter's identifier.</summary>
    public Guid HunterId { get; private set; }

    /// <summary>Gets the target's identifier.</summary>
    public Guid TargetId { get; private set; }

    /// <summary>Gets the fulfilled condition identifier.</summary>
    public Guid ConditionId { get; private set; }

    /// <summary>Gets the submission status.</summary>
    public TagStatus Status { get; private set; }

    /// <summary>Gets the time at which the tag was submitted.</summary>
    public DateTimeOffset SubmittedAt { get; private set; }

    /// <summary>Gets the resolution time, or null while pending.</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>Confirms the tag.</summary>
    public void Confirm(DateTimeOffset? resolvedAt = null) => Resolve(TagStatus.Confirmed, resolvedAt);

    /// <summary>Denies the tag.</summary>
    public void Deny(DateTimeOffset? resolvedAt = null) => Resolve(TagStatus.Denied, resolvedAt);

    /// <summary>Voids the tag.</summary>
    public void Void(DateTimeOffset? resolvedAt = null) => Resolve(TagStatus.Voided, resolvedAt);

    private void Resolve(TagStatus status, DateTimeOffset? resolvedAt)
    {
        if (Status != TagStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending tag can be resolved.");
        }

        Status = status;
        ResolvedAt = resolvedAt ?? DateTimeOffset.UtcNow;
    }

    private static void ValidateIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A non-empty identifier is required.", parameterName);
        }
    }
}
