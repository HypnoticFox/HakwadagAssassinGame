using HakwadagAssassinGame.Core.Enums;
using System.Text.Json.Serialization;

namespace HakwadagAssassinGame.Core.Entities.Conditions;

/// <summary>A creator-defined assignment condition.</summary>
public sealed class CustomCondition : Condition
{
    [JsonConstructor]
    public CustomCondition(string description, Guid id)
        : base(ConditionType.Custom, id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        Description = description;
    }

    /// <summary>Creates a custom condition.</summary>
    public static CustomCondition Create(string description, Guid? id = null) =>
        new(description, id ?? Guid.NewGuid());

    /// <summary>Gets the condition description.</summary>
    public string Description { get; private set; }

    /// <inheritdoc />
    public override string Describe() => Description;
}
