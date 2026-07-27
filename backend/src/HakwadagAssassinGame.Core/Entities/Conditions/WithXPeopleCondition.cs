using HakwadagAssassinGame.Core.Enums;
using System.Text.Json.Serialization;

namespace HakwadagAssassinGame.Core.Entities.Conditions;

/// <summary>A condition requiring the hunter to be with a minimum number of people.</summary>
public sealed class WithXPeopleCondition : Condition
{
    [JsonConstructor]
    public WithXPeopleCondition(int minPeople, Guid id)
        : base(ConditionType.WithXPeople, id)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minPeople);
        MinPeople = minPeople;
    }

    /// <summary>Creates a condition requiring at least <paramref name="minPeople"/> people.</summary>
    public static WithXPeopleCondition Create(int minPeople, Guid? id = null) =>
        new(minPeople, id ?? Guid.NewGuid());

    /// <summary>Gets the minimum number of people required.</summary>
    public int MinPeople { get; private set; }

    /// <inheritdoc />
    public override string Describe() => $"With at least {MinPeople} people";
}
