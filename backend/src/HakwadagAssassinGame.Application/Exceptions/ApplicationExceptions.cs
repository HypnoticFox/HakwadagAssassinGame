namespace HakwadagAssassinGame.Application.Exceptions;

/// <summary>Base exception for application validation failures.</summary>
public abstract class ApplicationExceptionBase : Exception
{
    /// <summary>Initializes an application exception.</summary>
    protected ApplicationExceptionBase(string message) : base(message) { }
}

/// <summary>Thrown when a requested game does not exist.</summary>
public sealed class GameNotFoundException : ApplicationExceptionBase
{
    /// <summary>Initializes the exception.</summary>
    public GameNotFoundException(Guid gameId) : base($"Game '{gameId}' was not found.") { }
}

/// <summary>Thrown when a requested player does not exist.</summary>
public sealed class PlayerNotFoundException : ApplicationExceptionBase
{
    /// <summary>Initializes the exception.</summary>
    public PlayerNotFoundException(Guid playerId) : base($"Player '{playerId}' was not found.") { }
}

/// <summary>Thrown when a requested assignment does not exist.</summary>
public sealed class AssignmentNotFoundException : ApplicationExceptionBase
{
    /// <summary>Initializes the exception.</summary>
    public AssignmentNotFoundException(Guid assignmentId) : base($"Assignment '{assignmentId}' was not found.") { }
}

/// <summary>Thrown when a requested tag submission does not exist.</summary>
public sealed class TagSubmissionNotFoundException : ApplicationExceptionBase
{
    /// <summary>Initializes the exception.</summary>
    public TagSubmissionNotFoundException(Guid tagId) : base($"Tag submission '{tagId}' was not found.") { }
}

/// <summary>Thrown when an operation is not valid for the current game state.</summary>
public sealed class InvalidGameStateException : ApplicationExceptionBase
{
    /// <summary>Initializes the exception.</summary>
    public InvalidGameStateException(string message) : base(message) { }
}

/// <summary>Thrown when a player lacks permission for an operation.</summary>
public sealed class UnauthorizedException : ApplicationExceptionBase
{
    /// <summary>Initializes the exception.</summary>
    public UnauthorizedException(string message = "The player is not authorized for this operation.") : base(message) { }
}

/// <summary>Thrown when a tag is submitted during a safe-time block.</summary>
public sealed class SafeTimeBlockViolationException : ApplicationExceptionBase
{
    /// <summary>Initializes the exception.</summary>
    public SafeTimeBlockViolationException() : base("Tags cannot be submitted during safe time.") { }
}

/// <summary>Thrown when a target already has an unresolved tag.</summary>
public sealed class PendingTagExistsException : ApplicationExceptionBase
{
    /// <summary>Initializes the exception.</summary>
    public PendingTagExistsException(Guid targetId) : base($"Player '{targetId}' already has a pending tag.") { }
}
