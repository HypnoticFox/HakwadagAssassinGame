using System.Text.Json.Serialization;

namespace HakwadagAssassinGame.Core.Entities;

/// <summary>Represents a recurring period during which tags cannot be submitted.</summary>
public sealed class SafeTimeBlock
{
    [JsonConstructor]
    public SafeTimeBlock(DateTimeOffset startTime, DateTimeOffset endTime, Guid id)
    {
        Id = id;
        StartTime = startTime;
        EndTime = endTime;
    }

    /// <summary>Creates a safe-time block. End times earlier than start times cross midnight.</summary>
    public static SafeTimeBlock Create(DateTimeOffset startTime, DateTimeOffset endTime, Guid? id = null) =>
        new(startTime, endTime, id ?? Guid.NewGuid());

    /// <summary>Gets the identifier of the block.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the start time of the block (with the creator's timezone offset).</summary>
    public DateTimeOffset StartTime { get; private set; }

    /// <summary>Gets the end time of the block (with the creator's timezone offset).</summary>
    public DateTimeOffset EndTime { get; private set; }

    /// <summary>Determines whether the supplied UTC instant falls within this block.</summary>
    public bool Contains(DateTimeOffset instant)
    {
        // Convert the instant to the block's timezone offset
        var offset = StartTime.Offset;
        var instantInBlockOffset = instant.ToOffset(offset);
        var instantTime = instantInBlockOffset.TimeOfDay;

        var startTime = StartTime.TimeOfDay;
        var endTime = EndTime.ToOffset(offset).TimeOfDay;

        return startTime <= endTime
            ? instantTime >= startTime && instantTime < endTime
            : instantTime >= startTime || instantTime < endTime;
    }
}
