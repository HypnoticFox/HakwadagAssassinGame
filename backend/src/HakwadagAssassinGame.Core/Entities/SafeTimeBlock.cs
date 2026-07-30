using System.Text.Json.Serialization;

namespace HakwadagAssassinGame.Core.Entities;

/// <summary>Represents a recurring period during which tags cannot be submitted.</summary>
public sealed class SafeTimeBlock
{
    [JsonConstructor]
    public SafeTimeBlock(TimeSpan startTime, TimeSpan endTime, DayOfWeek? day, Guid id)
    {
        ValidateTime(startTime, nameof(startTime));
        ValidateTime(endTime, nameof(endTime));
        Id = id;
        StartTime = startTime;
        EndTime = endTime;
        Day = day;
    }

    /// <summary>Creates a safe-time block. End times earlier than start times cross midnight.</summary>
    public static SafeTimeBlock Create(TimeSpan startTime, TimeSpan endTime, DayOfWeek? day = null, Guid? id = null) =>
        new(startTime, endTime, day, id ?? Guid.NewGuid());

    /// <summary>Gets the identifier of the block.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the start time of the block.</summary>
    public TimeSpan StartTime { get; private set; }

    /// <summary>Gets the end time of the block.</summary>
    public TimeSpan EndTime { get; private set; }

    /// <summary>Gets the day to which the block applies, or all days when null.</summary>
    public DayOfWeek? Day { get; private set; }

    /// <summary>Determines whether the supplied instant falls within this block.</summary>
    public bool Contains(DateTimeOffset instant)
    {
        if (Day.HasValue && Day.Value != instant.DayOfWeek)
        {
            return false;
        }

        var time = instant.TimeOfDay;
        return StartTime <= EndTime
            ? time >= StartTime && time < EndTime
            : time >= StartTime || time < EndTime;
    }

    private static void ValidateTime(TimeSpan time, string parameterName)
    {
        if (time < TimeSpan.Zero || time >= TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(parameterName, "A time must be between 00:00 and 23:59:59.9999999.");
        }
    }
}
