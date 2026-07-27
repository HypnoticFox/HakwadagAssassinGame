namespace HakwadagAssassinGame.Core.Enums;

/// <summary>Represents the lifecycle state of a hunter-target assignment.</summary>
public enum AssignmentStatus
{
    /// <summary>The assignment can be hunted.</summary>
    Active,

    /// <summary>The hunter completed the assignment.</summary>
    Completed,

    /// <summary>The assignment was invalidated by an administrator or game action.</summary>
    Voided,

    /// <summary>The target left the game.</summary>
    TargetLeft
}
