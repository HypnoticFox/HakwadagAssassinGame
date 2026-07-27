using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;
using System.Text.Json.Serialization;

namespace HakwadagAssassinGame.Core.Entities;

/// <summary>Represents a hunter's current target and its required conditions.</summary>
public sealed class Assignment
{
    [JsonConstructor]
    public Assignment(
        Guid id,
        Guid gameId,
        Guid hunterId,
        Guid targetId,
        DateTimeOffset assignedAt,
        List<Condition> conditions)
    {
        ValidateIdentifier(gameId, nameof(gameId));
        ValidateIdentifier(hunterId, nameof(hunterId));
        ValidateIdentifier(targetId, nameof(targetId));
        if (hunterId == targetId)
        {
            throw new ArgumentException("A player cannot be assigned to hunt themselves.", nameof(targetId));
        }

        var conditionList = conditions?.ToList() ?? throw new ArgumentNullException(nameof(conditions));
        if (conditionList.Count == 0)
        {
            throw new ArgumentException("An assignment must have at least one condition.", nameof(conditions));
        }

        Id = id;
        GameId = gameId;
        HunterId = hunterId;
        TargetId = targetId;
        Status = AssignmentStatus.Active;
        AssignedAt = assignedAt;
        Conditions = conditionList;
    }

    /// <summary>Creates an active assignment.</summary>
    public static Assignment Create(
        Guid gameId,
        Guid hunterId,
        Guid targetId,
        IEnumerable<Condition> conditions,
        Guid? id = null,
        DateTimeOffset? assignedAt = null) => new(
            id ?? Guid.NewGuid(),
            gameId,
            hunterId,
            targetId,
            assignedAt ?? DateTimeOffset.UtcNow,
            conditions.ToList());

    /// <summary>Gets the assignment identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the game identifier.</summary>
    public Guid GameId { get; private set; }

    /// <summary>Gets the hunter's player identifier.</summary>
    public Guid HunterId { get; private set; }

    /// <summary>Gets the target's player identifier.</summary>
    public Guid TargetId { get; private set; }

    /// <summary>Gets the assignment lifecycle status.</summary>
    public AssignmentStatus Status { get; private set; }

    /// <summary>Gets the time at which the assignment was made.</summary>
    public DateTimeOffset AssignedAt { get; private set; }

    /// <summary>Gets the circumstances that can be fulfilled for this assignment.</summary>
    public List<Condition> Conditions { get; private set; }

    /// <summary>Marks the assignment as completed.</summary>
    public void Complete() => SetStatus(AssignmentStatus.Completed);

    /// <summary>Voids the assignment.</summary>
    public void Void() => SetStatus(AssignmentStatus.Voided);

    /// <summary>Marks the assignment as invalid because its target left.</summary>
    public void MarkTargetLeft() => SetStatus(AssignmentStatus.TargetLeft);

    private void SetStatus(AssignmentStatus status)
    {
        if (Status != AssignmentStatus.Active)
        {
            throw new InvalidOperationException("Only an active assignment can change status.");
        }

        Status = status;
    }

    private static void ValidateIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A non-empty identifier is required.", parameterName);
        }
    }
}
