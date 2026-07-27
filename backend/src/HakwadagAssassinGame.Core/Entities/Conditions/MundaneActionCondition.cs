using HakwadagAssassinGame.Core.Enums;
using System.Text.Json.Serialization;

namespace HakwadagAssassinGame.Core.Entities.Conditions;

/// <summary>A condition requiring a mundane action to be taking place.</summary>
public sealed class MundaneActionCondition : Condition
{
    [JsonConstructor]
    public MundaneActionCondition(string action, Guid id)
        : base(ConditionType.MundaneAction, id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        Action = action;
    }

    /// <summary>Creates a mundane-action condition.</summary>
    public static MundaneActionCondition Create(string action, Guid? id = null) =>
        new(action, id ?? Guid.NewGuid());

    /// <summary>Gets the action required by the condition.</summary>
    public string Action { get; private set; }

    /// <inheritdoc />
    public override string Describe() => Action;
}
