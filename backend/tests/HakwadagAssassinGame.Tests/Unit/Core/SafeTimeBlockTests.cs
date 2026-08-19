using HakwadagAssassinGame.Core.Entities;

namespace HakwadagAssassinGame.Tests.Unit.Core;

public sealed class SafeTimeBlockTests
{
    private static readonly TimeSpan CestOffset = TimeSpan.FromHours(2);

    // ── Create validation ──────────────────────────────────────────────────

    [Fact]
    public void Create_ValidInputs_SetsProperties()
    {
        var id = Guid.NewGuid();
        var startTime = new DateTimeOffset(2025, 6, 15, 22, 0, 0, CestOffset);
        var endTime = new DateTimeOffset(2025, 6, 15, 6, 0, 0, CestOffset);

        var block = SafeTimeBlock.Create(startTime, endTime, id);

        Assert.Equal(id, block.Id);
        Assert.Equal(startTime, block.StartTime);
        Assert.Equal(endTime, block.EndTime);
    }

    [Fact]
    public void Create_DefaultId_IsNotEmpty()
    {
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 22, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 6, 0, 0, CestOffset));
        Assert.NotEqual(Guid.Empty, block.Id);
    }

    [Fact]
    public void Constructor_WithJsonConstructor_SetsProperties()
    {
        var id = Guid.NewGuid();
        var startTime = new DateTimeOffset(2025, 6, 15, 8, 0, 0, CestOffset);
        var endTime = new DateTimeOffset(2025, 6, 15, 16, 0, 0, CestOffset);

        var block = new SafeTimeBlock(startTime, endTime, id);

        Assert.Equal(id, block.Id);
        Assert.Equal(startTime, block.StartTime);
        Assert.Equal(endTime, block.EndTime);
    }

    // ── Contains — same-day block (start < end) ────────────────────────────
    // Block: 09:00+02:00 → 17:00+02:00, which is 07:00-15:00 UTC.

    [Fact]
    public void Contains_SameDayBlock_TimeWithinRange_ReturnsTrue()
    {
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 9, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 17, 0, 0, CestOffset));
        // 10:30 UTC == 12:30+02:00 → inside
        var instant = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_SameDayBlock_TimeAtStart_ReturnsTrue()
    {
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 9, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 17, 0, 0, CestOffset));
        // 07:00 UTC == 09:00+02:00 → inclusive start
        var instant = new DateTimeOffset(2025, 6, 15, 7, 0, 0, TimeSpan.Zero);
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_SameDayBlock_TimeAtEndExclusive_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 9, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 17, 0, 0, CestOffset));
        // 15:00 UTC == 17:00+02:00 → exclusive end
        var instant = new DateTimeOffset(2025, 6, 15, 15, 0, 0, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    [Fact]
    public void Contains_SameDayBlock_TimeBeforeStart_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 9, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 17, 0, 0, CestOffset));
        // 06:59 UTC == 08:59+02:00 → before start
        var instant = new DateTimeOffset(2025, 6, 15, 6, 59, 0, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    [Fact]
    public void Contains_SameDayBlock_TimeAfterEnd_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 9, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 17, 0, 0, CestOffset));
        // 15:01 UTC == 17:01+02:00 → after end
        var instant = new DateTimeOffset(2025, 6, 15, 15, 1, 0, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    // ── Contains — cross-midnight block (start > end) ──────────────────────
    // Block: 22:00+02:00 → 06:00+02:00, which is 20:00-04:00 UTC.

    [Fact]
    public void Contains_CrossMidnightBlock_TimeAfterStart_ReturnsTrue()
    {
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 22, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 6, 0, 0, CestOffset));
        // 21:30 UTC == 23:30+02:00 → inside
        var instant = new DateTimeOffset(2025, 6, 15, 21, 30, 0, TimeSpan.Zero);
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_CrossMidnightBlock_TimeBeforeEndNextDay_ReturnsTrue()
    {
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 22, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 6, 0, 0, CestOffset));
        // 01:00 UTC == 03:00+02:00 next day → inside
        var instant = new DateTimeOffset(2025, 6, 16, 1, 0, 0, TimeSpan.Zero);
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_CrossMidnightBlock_TimeAtStart_ReturnsTrue()
    {
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 22, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 6, 0, 0, CestOffset));
        // 20:00 UTC == 22:00+02:00 → inclusive start
        var instant = new DateTimeOffset(2025, 6, 15, 20, 0, 0, TimeSpan.Zero);
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_CrossMidnightBlock_TimeAtEnd_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 22, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 6, 0, 0, CestOffset));
        // 04:00 UTC == 06:00+02:00 → exclusive end
        var instant = new DateTimeOffset(2025, 6, 16, 4, 0, 0, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    [Fact]
    public void Contains_CrossMidnightBlock_TimeInGap_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 22, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 6, 0, 0, CestOffset));
        // 12:00 UTC == 14:00+02:00 → in the gap
        var instant = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    [Fact]
    public void Contains_CrossMidnightBlock_TimeBeforeStart_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 22, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 6, 0, 0, CestOffset));
        // 19:59 UTC == 21:59+02:00 → before start
        var instant = new DateTimeOffset(2025, 6, 15, 19, 59, 0, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    // ── Contains — any day ─────────────────────────────────────────────────

    [Fact]
    public void Contains_Block_MatchesAnyDay()
    {
        // Block: 10:00+02:00 → 18:00+02:00 (08:00-16:00 UTC). Applies every day.
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 10, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 18, 0, 0, CestOffset));
        var monday = new DateTimeOffset(2025, 6, 16, 12, 0, 0, TimeSpan.Zero); // 14:00+02:00
        var sunday = new DateTimeOffset(2025, 6, 22, 12, 0, 0, TimeSpan.Zero); // 14:00+02:00
        Assert.True(block.Contains(monday));
        Assert.True(block.Contains(sunday));
    }

    // ── Contains — edge cases ──────────────────────────────────────────────

    [Fact]
    public void Contains_ZeroLengthBlock_AtStart_ReturnsFalse()
    {
        // Start == End; no time is within [start, end)
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 12, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 12, 0, 0, CestOffset));
        // 10:00 UTC == 12:00+02:00
        var instant = new DateTimeOffset(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    [Fact]
    public void Contains_FullDayBlock_AnyTime_ReturnsTrue()
    {
        // 00:00+02:00 to 23:59:59.9999999+02:00 covers the whole day.
        var start = new DateTimeOffset(2025, 6, 15, 0, 0, 0, CestOffset);
        var end = start.AddTicks(TimeSpan.TicksPerDay - 1);
        var block = SafeTimeBlock.Create(start, end);
        var instant = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.Zero); // 16:00+02:00
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_MidnightExact_DifferentInterpretations()
    {
        // 22:00+02:00 → 06:00+02:00 cross-midnight. Midnight UTC (00:00 = 02:00+02:00) is inside.
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 22, 0, 0, CestOffset),
            new DateTimeOffset(2025, 6, 15, 6, 0, 0, CestOffset));
        var midnight = new DateTimeOffset(2025, 6, 16, 0, 0, 0, TimeSpan.Zero);
        Assert.True(block.Contains(midnight));
    }

    // ── Contains — different offset ────────────────────────────────────────

    [Fact]
    public void Contains_DifferentOffset_ConvertsCorrectly()
    {
        // Block uses a -05:00 offset; instants are supplied in UTC.
        var block = SafeTimeBlock.Create(
            new DateTimeOffset(2025, 6, 15, 9, 0, 0, TimeSpan.FromHours(-5)),
            new DateTimeOffset(2025, 6, 15, 17, 0, 0, TimeSpan.FromHours(-5)));

        // 14:30 UTC == 09:30-05:00 → inside the block
        Assert.True(block.Contains(new DateTimeOffset(2025, 6, 15, 14, 30, 0, TimeSpan.Zero)));

        // 13:30 UTC == 08:30-05:00 → before the block starts
        Assert.False(block.Contains(new DateTimeOffset(2025, 6, 15, 13, 30, 0, TimeSpan.Zero)));
    }
}
