namespace HakwadagAssassinGame.Core.Enums;

/// <summary>Identifies a built-in assignment condition.</summary>
public enum ConditionType
{
    /// <summary>The hunter must be with a specified person.</summary>
    WithSpecificPerson,

    /// <summary>The hunter must be alone with the target.</summary>
    Alone,

    /// <summary>The hunter must be with at least a specified number of people.</summary>
    WithXPeople,

    /// <summary>The hunter must perform a specified mundane action.</summary>
    MundaneAction,

    /// <summary>The hunter must satisfy a creator-defined condition.</summary>
    Custom
}
