using HakwadagAssassinGame.Core.Enums;
using System.Text.Json.Serialization;

namespace HakwadagAssassinGame.Core.Entities.Conditions;

/// <summary>A condition requiring the hunter to be alone with the target.</summary>
public sealed class AloneCondition : Condition
{
    [JsonConstructor]
    public AloneCondition(Guid id)
        : base(ConditionType.Alone, id)
    {
    }

    /// <summary>Creates an alone condition.</summary>
    public static AloneCondition Create(Guid? id = null) => new(id ?? Guid.NewGuid());

    /// <inheritdoc />
    public override string Describe() => "Alone";
}
