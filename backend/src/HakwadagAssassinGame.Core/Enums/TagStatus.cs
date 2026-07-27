namespace HakwadagAssassinGame.Core.Enums;

/// <summary>Represents the state of a submitted tag.</summary>
public enum TagStatus
{
    /// <summary>The target has not resolved the submission.</summary>
    Pending,

    /// <summary>The target confirmed the submission, or it timed out.</summary>
    Confirmed,

    /// <summary>The target denied the submission.</summary>
    Denied,

    /// <summary>The submission was invalidated by an administrator or game action.</summary>
    Voided
}
