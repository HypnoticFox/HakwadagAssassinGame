using HakwadagAssassinGame.Core.Enums;
using System.Text.Json.Serialization;

namespace HakwadagAssassinGame.Core.Entities.Conditions;

/// <summary>A condition requiring the hunter to be with a particular person.</summary>
public sealed class WithSpecificPersonCondition : Condition
{
    [JsonConstructor]
    public WithSpecificPersonCondition(Guid? targetPersonId, Guid id)
        : base(ConditionType.WithSpecificPerson, id)
    {
        TargetPersonId = targetPersonId;
    }

    /// <summary>Creates a condition for a particular person.</summary>
    public static WithSpecificPersonCondition Create(Guid? targetPersonId, Guid? id = null) =>
        new(targetPersonId, id ?? Guid.NewGuid());

    /// <summary>Gets the person who must be present, when one has been selected.</summary>
    public Guid? TargetPersonId { get; private set; }

    /// <inheritdoc />
    public override string Describe() => TargetPersonId.HasValue
        ? $"With specific person ({TargetPersonId.Value})"
        : "With a specific person";
}
