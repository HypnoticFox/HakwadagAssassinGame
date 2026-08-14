using HakwadagAssassinGame.Core.Enums;
using System.Text.Json.Serialization;

namespace HakwadagAssassinGame.Core.Entities;

/// <summary>Represents a temporary Hakwadag game.</summary>
public sealed class Game
{
    [JsonConstructor]
    public Game(
        Guid id,
        string name,
        string inviteCode,
        GameStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? scheduledEndAt,
        DateTimeOffset? endedAt,
        int maxPlayers,
        int basePointsPerTag,
        Dictionary<ConditionType, int> conditionBonuses,
        TimeSpan confirmationTimeout,
        int assignmentCooldownMinutes,
        List<SafeTimeBlock> safeTimeBlocks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(inviteCode);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPlayers, 3);
        ArgumentOutOfRangeException.ThrowIfNegative(basePointsPerTag);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(confirmationTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegative(assignmentCooldownMinutes);

        Id = id;
        Name = name;
        InviteCode = inviteCode;
        Status = status;
        CreatedAt = createdAt;
        ScheduledEndAt = scheduledEndAt;
        EndedAt = endedAt;
        MaxPlayers = maxPlayers;
        BasePointsPerTag = basePointsPerTag;
        ConditionBonuses = conditionBonuses is null
            ? new Dictionary<ConditionType, int>()
            : new Dictionary<ConditionType, int>(conditionBonuses);
        if (ConditionBonuses.Values.Any(static bonus => bonus < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(conditionBonuses), "Condition bonuses cannot be negative.");
        }

        ConfirmationTimeout = confirmationTimeout;
        AssignmentCooldownMinutes = assignmentCooldownMinutes;
        SafeTimeBlocks = safeTimeBlocks?.ToList() ?? [];
    }

    /// <summary>Creates a game in the not-started state.</summary>
    public static Game Create(
        string name,
        string inviteCode,
        DateTimeOffset? scheduledEndAt,
        int maxPlayers,
        int basePointsPerTag,
        IDictionary<ConditionType, int>? conditionBonuses = null,
        TimeSpan? confirmationTimeout = null,
        IEnumerable<SafeTimeBlock>? safeTimeBlocks = null,
        Guid? id = null,
        DateTimeOffset? createdAt = null,
        int? assignmentCooldownMinutes = null) => new(
            id ?? Guid.NewGuid(),
            name,
            inviteCode,
            GameStatus.NotStarted,
            createdAt ?? DateTimeOffset.UtcNow,
            scheduledEndAt,
            null,
            maxPlayers,
            basePointsPerTag,
            conditionBonuses is null ? new Dictionary<ConditionType, int>() : new Dictionary<ConditionType, int>(conditionBonuses),
            confirmationTimeout ?? TimeSpan.FromMinutes(5),
            assignmentCooldownMinutes ?? 30,
            safeTimeBlocks?.ToList() ?? []);

    /// <summary>Gets the game identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the display name of the game.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the code players use to join the game.</summary>
    public string InviteCode { get; private set; }

    /// <summary>Gets the lifecycle status.</summary>
    public GameStatus Status { get; private set; }

    /// <summary>Gets the time at which the game was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets the scheduled end time, or null when the game has no fixed end time.</summary>
    public DateTimeOffset? ScheduledEndAt { get; private set; }

    /// <summary>Gets the actual end time, or null while the game is running.</summary>
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>Gets the maximum number of players.</summary>
    public int MaxPlayers { get; private set; }

    /// <summary>Gets the base score awarded for a confirmed tag.</summary>
    public int BasePointsPerTag { get; private set; }

    /// <summary>Gets the score bonus for each condition type.</summary>
    public Dictionary<ConditionType, int> ConditionBonuses { get; private set; }

    /// <summary>Gets the time before an unresolved tag is automatically confirmed.</summary>
    public TimeSpan ConfirmationTimeout { get; private set; }

    /// <summary>Gets the minimum number of minutes a player must wait between assignments. Zero disables the cooldown.</summary>
    public int AssignmentCooldownMinutes { get; private set; }

    /// <summary>Gets the periods during which tags are not allowed.</summary>
    public List<SafeTimeBlock> SafeTimeBlocks { get; private set; }

    /// <summary>Starts the game.</summary>
    public void Start()
    {
        if (Status != GameStatus.NotStarted)
        {
            throw new InvalidOperationException("Only a game that has not started can be started.");
        }

        Status = GameStatus.Active;
    }

    /// <summary>Sets the scheduled end time. Only valid before the game starts.</summary>
    public void SetScheduledEnd(DateTimeOffset? scheduledEndAt)
    {
        if (Status != GameStatus.NotStarted)
        {
            throw new InvalidOperationException("The scheduled end time can only be changed before the game starts.");
        }
        ScheduledEndAt = scheduledEndAt;
    }

    /// <summary>Extends the scheduled end time by the given duration. Only valid while the game is active.</summary>
    public void ExtendTime(TimeSpan extension)
    {
        if (Status != GameStatus.Active)
        {
            throw new InvalidOperationException("Time can only be extended while the game is active.");
        }
        if (extension <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(extension), "Extension must be positive.");
        }
        ScheduledEndAt = (ScheduledEndAt ?? DateTimeOffset.UtcNow) + extension;
    }

    /// <summary>Ends the game at the supplied time.</summary>
    public void End(DateTimeOffset? endedAt = null)
    {
        if (Status == GameStatus.Ended)
        {
            return;
        }

        Status = GameStatus.Ended;
        EndedAt = endedAt ?? DateTimeOffset.UtcNow;
    }

    /// <summary>Updates the confirmation timeout. Only valid while the game is active.</summary>
    public void UpdateConfirmationTimeout(TimeSpan timeout)
    {
        if (Status != GameStatus.Active)
        {
            throw new InvalidOperationException("The confirmation timeout can only be changed while the game is active.");
        }
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");
        }
        ConfirmationTimeout = timeout;
    }

    /// <summary>Updates the assignment cooldown. Only valid while the game is active.</summary>
    public void UpdateAssignmentCooldown(int minutes)
    {
        if (Status != GameStatus.Active)
        {
            throw new InvalidOperationException("The assignment cooldown can only be changed while the game is active.");
        }
        if (minutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes), "Cooldown cannot be negative.");
        }
        AssignmentCooldownMinutes = minutes;
    }
}
