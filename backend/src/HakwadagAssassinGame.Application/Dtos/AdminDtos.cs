namespace HakwadagAssassinGame.Application.Dtos;

/// <summary>Identifies a player to promote to co-admin.</summary>
public record AddAdminRequest(Guid PlayerId);

/// <summary>Describes a safe-time block to add.</summary>
public record AddSafeTimeBlockRequest(TimeSpan StartTime, TimeSpan EndTime, DayOfWeek? Day);

/// <summary>Describes a custom condition to add to a game's condition library.</summary>
public record AddCustomConditionRequest(string Description);

/// <summary>Updates the scheduled duration of a game before it starts.</summary>
public record UpdateDurationRequest(int DurationHours);

/// <summary>Extends the remaining time of an active game.</summary>
public record ExtendDurationRequest(int Minutes);
