using HakwadagAssassinGame.Core.Enums;
using System.Text.Json.Serialization;

namespace HakwadagAssassinGame.Core.Entities.Conditions;

/// <summary>Base type for a circumstance that can be attached to an assignment.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(WithSpecificPersonCondition), "withSpecificPerson")]
[JsonDerivedType(typeof(AloneCondition), "alone")]
[JsonDerivedType(typeof(WithXPeopleCondition), "withXPeople")]
[JsonDerivedType(typeof(MundaneActionCondition), "mundaneAction")]
[JsonDerivedType(typeof(CustomCondition), "custom")]
public abstract class Condition
{
    /// <summary>Initializes a condition.</summary>
    /// <param name="type">The kind of condition.</param>
    /// <param name="id">The condition identifier.</param>
    protected Condition(ConditionType type, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        Type = type;
    }

    /// <summary>Gets the unique identifier of the condition.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the kind of condition.</summary>
    public ConditionType Type { get; private set; }

    /// <summary>Returns a human-readable description of the condition.</summary>
    public abstract string Describe();
}
