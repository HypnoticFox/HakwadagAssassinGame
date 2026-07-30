using HakwadagAssassinGame.Core.Entities;

namespace HakwadagAssassinGame.Tests.Unit.Core;

public sealed class SafeTimeBlockTests
{
    // ── Create validation ──────────────────────────────────────────────────

    [Fact]
    public void Create_NegativeStartTime_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SafeTimeBlock.Create(TimeSpan.FromHours(-1), TimeSpan.FromHours(6)));
        Assert.Contains("startTime", ex.Message);
    }

    [Fact]
    public void Create_StartTimeEqualTo24Hours_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SafeTimeBlock.Create(TimeSpan.FromDays(1), TimeSpan.FromHours(6)));
        Assert.Contains("startTime", ex.Message);
    }

    [Fact]
    public void Create_NegativeEndTime_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(-1)));
        Assert.Contains("endTime", ex.Message);
    }

    [Fact]
    public void Create_EndTimeEqualTo24Hours_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromDays(1)));
        Assert.Contains("endTime", ex.Message);
    }

    [Fact]
    public void Create_ValidInputs_SetsProperties()
    {
        var id = Guid.NewGuid();
        var block = SafeTimeBlock.Create(
            TimeSpan.FromHours(22), TimeSpan.FromHours(6), DayOfWeek.Saturday, id);

        Assert.Equal(id, block.Id);
        Assert.Equal(TimeSpan.FromHours(22), block.StartTime);
        Assert.Equal(TimeSpan.FromHours(6), block.EndTime);
        Assert.Equal(DayOfWeek.Saturday, block.Day);
    }

    [Fact]
    public void Create_NullDay_AppliesToAllDays()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(6));
        Assert.Null(block.Day);
    }

    [Fact]
    public void Create_DefaultId_IsNotEmpty()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(6));
        Assert.NotEqual(Guid.Empty, block.Id);
    }

    [Fact]
    public void Create_MaxValidTime_DoesNotThrow()
    {
        // 23:59:59.9999999 is the maximum valid time (< 24:00)
        var maxTime = TimeSpan.FromDays(1) - TimeSpan.FromTicks(1);
        var block = SafeTimeBlock.Create(maxTime, maxTime);
        Assert.Equal(maxTime, block.StartTime);
    }

    [Fact]
    public void Create_MinValidTime_DoesNotThrow()
    {
        var block = SafeTimeBlock.Create(TimeSpan.Zero, TimeSpan.Zero);
        Assert.Equal(TimeSpan.Zero, block.StartTime);
    }

    // ── Contains — same-day block (start < end) ────────────────────────────

    [Fact]
    public void Contains_SameDayBlock_TimeWithinRange_ReturnsTrue()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        var instant = new DateTimeOffset(2025, 6, 15, 12, 30, 0, TimeSpan.Zero);
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_SameDayBlock_TimeAtStart_ReturnsTrue()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        var instant = new DateTimeOffset(2025, 6, 15, 9, 0, 0, TimeSpan.Zero);
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_SameDayBlock_TimeAtEndExclusive_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        var instant = new DateTimeOffset(2025, 6, 15, 17, 0, 0, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    [Fact]
    public void Contains_SameDayBlock_TimeBeforeStart_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        var instant = new DateTimeOffset(2025, 6, 15, 8, 59, 59, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    [Fact]
    public void Contains_SameDayBlock_TimeAfterEnd_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        var instant = new DateTimeOffset(2025, 6, 15, 17, 0, 1, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    // ── Contains — cross-midnight block (start > end) ──────────────────────

    [Fact]
    public void Contains_CrossMidnightBlock_TimeAfterStart_ReturnsTrue()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(6));
        var instant = new DateTimeOffset(2025, 6, 15, 23, 30, 0, TimeSpan.Zero);
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_CrossMidnightBlock_TimeBeforeEndNextDay_ReturnsTrue()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(6));
        var instant = new DateTimeOffset(2025, 6, 16, 3, 0, 0, TimeSpan.Zero);
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_CrossMidnightBlock_TimeAtStart_ReturnsTrue()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(6));
        var instant = new DateTimeOffset(2025, 6, 15, 22, 0, 0, TimeSpan.Zero);
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_CrossMidnightBlock_TimeAtEnd_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(6));
        var instant = new DateTimeOffset(2025, 6, 16, 6, 0, 0, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    [Fact]
    public void Contains_CrossMidnightBlock_TimeInGap_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(6));
        var instant = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    [Fact]
    public void Contains_CrossMidnightBlock_TimeBeforeStart_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(6));
        var instant = new DateTimeOffset(2025, 6, 15, 21, 59, 59, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    // ── Contains — day-specific blocks ─────────────────────────────────────

    [Fact]
    public void Contains_DaySpecificBlock_MatchingDay_ReturnsTrue()
    {
        var block = SafeTimeBlock.Create(
            TimeSpan.FromHours(10), TimeSpan.FromHours(18), DayOfWeek.Monday);
        // 2025-06-16 is a Monday
        var instant = new DateTimeOffset(2025, 6, 16, 14, 0, 0, TimeSpan.Zero);
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_DaySpecificBlock_NonMatchingDay_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(
            TimeSpan.FromHours(10), TimeSpan.FromHours(18), DayOfWeek.Monday);
        // 2025-06-17 is a Tuesday
        var instant = new DateTimeOffset(2025, 6, 17, 14, 0, 0, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    [Fact]
    public void Contains_DaySpecificBlock_CrossMidnight_MatchingDay_ReturnsTrue()
    {
        var block = SafeTimeBlock.Create(
            TimeSpan.FromHours(22), TimeSpan.FromHours(6), DayOfWeek.Friday);
        // Thursday → Friday midnight: instant on Friday at 3am should match
        var instant = new DateTimeOffset(2025, 6, 20, 3, 0, 0, TimeSpan.Zero); // Friday
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_DaySpecificBlock_CrossMidnight_NonMatchingDay_ReturnsFalse()
    {
        var block = SafeTimeBlock.Create(
            TimeSpan.FromHours(22), TimeSpan.FromHours(6), DayOfWeek.Friday);
        // Instant on Saturday at 3am should NOT match
        var instant = new DateTimeOffset(2025, 6, 21, 3, 0, 0, TimeSpan.Zero); // Saturday
        Assert.False(block.Contains(instant));
    }

    [Fact]
    public void Contains_AllDaysBlock_NullDay_MatchesAnyDay()
    {
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(10), TimeSpan.FromHours(18));
        var monday = new DateTimeOffset(2025, 6, 16, 14, 0, 0, TimeSpan.Zero);
        var sunday = new DateTimeOffset(2025, 6, 22, 14, 0, 0, TimeSpan.Zero);
        Assert.True(block.Contains(monday));
        Assert.True(block.Contains(sunday));
    }

    // ── Contains — edge cases ──────────────────────────────────────────────

    [Fact]
    public void Contains_ZeroLengthBlock_AtStart_ReturnsFalse()
    {
        // Start == End; no time is within [start, end)
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(12), TimeSpan.FromHours(12));
        var instant = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        Assert.False(block.Contains(instant));
    }

    [Fact]
    public void Contains_FullDayBlock_AnyTime_ReturnsTrue()
    {
        // 00:00 to 00:00 wraps the full day — but since start==end,
        // it's actually an empty block. Let's test a proper full-day:
        var block = SafeTimeBlock.Create(TimeSpan.Zero, TimeSpan.FromDays(1) - TimeSpan.FromTicks(1));
        var instant = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.Zero);
        Assert.True(block.Contains(instant));
    }

    [Fact]
    public void Contains_MidnightExact_DifferentInterpretations()
    {
        // 22:00 → 06:00 cross-midnight. Midnight (00:00) should be inside.
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(6));
        var midnight = new DateTimeOffset(2025, 6, 16, 0, 0, 0, TimeSpan.Zero);
        Assert.True(block.Contains(midnight));
    }

    // ── JSON Constructor ───────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithJsonConstructor_SetsProperties()
    {
        var id = Guid.NewGuid();
        var block = new SafeTimeBlock(TimeSpan.FromHours(8), TimeSpan.FromHours(16), DayOfWeek.Wednesday, id);

        Assert.Equal(id, block.Id);
        Assert.Equal(TimeSpan.FromHours(8), block.StartTime);
        Assert.Equal(TimeSpan.FromHours(16), block.EndTime);
        Assert.Equal(DayOfWeek.Wednesday, block.Day);
    }
}
