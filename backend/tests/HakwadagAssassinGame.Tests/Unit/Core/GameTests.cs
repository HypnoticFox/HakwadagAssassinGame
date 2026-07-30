using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Tests.Unit.Core;

public sealed class GameTests
{
    private static readonly DateTimeOffset DefaultEndAt = DateTimeOffset.UtcNow.AddDays(7);

    // ── Constructor / Create validation ────────────────────────────────────

    [Fact]
    public void Create_NullName_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Game.Create(null!, "CODE", DefaultEndAt, 4, 10));
        Assert.Contains("name", ex.Message);
    }

    [Fact]
    public void Create_EmptyName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Game.Create("", "CODE", DefaultEndAt, 4, 10));
        Assert.Contains("name", ex.Message);
    }

    [Fact]
    public void Create_WhitespaceName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Game.Create("   ", "CODE", DefaultEndAt, 4, 10));
        Assert.Contains("name", ex.Message);
    }

    [Fact]
    public void Create_NullInviteCode_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Game.Create("TestGame", null!, DefaultEndAt, 4, 10));
        Assert.Contains("inviteCode", ex.Message);
    }

    [Fact]
    public void Create_EmptyInviteCode_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Game.Create("TestGame", "", DefaultEndAt, 4, 10));
        Assert.Contains("inviteCode", ex.Message);
    }

    [Fact]
    public void Create_MaxPlayersLessThan2_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Game.Create("TestGame", "CODE", DefaultEndAt, 1, 10));
        Assert.Contains("maxPlayers", ex.Message);
    }

    [Fact]
    public void Create_NegativeBasePointsPerTag_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Game.Create("TestGame", "CODE", DefaultEndAt, 4, -1));
        Assert.Contains("basePointsPerTag", ex.Message);
    }

    [Fact]
    public void Create_ZeroConfirmationTimeout_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Game.Create("TestGame", "CODE", DefaultEndAt, 4, 10,
                confirmationTimeout: TimeSpan.Zero));
        Assert.Contains("confirmationTimeout", ex.Message);
    }

    [Fact]
    public void Create_NegativeConfirmationTimeout_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Game.Create("TestGame", "CODE", DefaultEndAt, 4, 10,
                confirmationTimeout: TimeSpan.FromMinutes(-1)));
        Assert.Contains("confirmationTimeout", ex.Message);
    }

    [Fact]
    public void Create_NegativeConditionBonus_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Game.Create("TestGame", "CODE", DefaultEndAt, 4, 10,
                conditionBonuses: new Dictionary<ConditionType, int>
                {
                    { ConditionType.Alone, -5 }
                }));
        Assert.Contains("conditionBonuses", ex.Message);
    }

    [Fact]
    public void Create_ValidInputs_SetsPropertiesCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var gameId = Guid.NewGuid();
        var endAt = now.AddDays(3);
        var bonuses = new Dictionary<ConditionType, int> { { ConditionType.Alone, 15 } };
        var safeBlocks = new List<SafeTimeBlock>
        {
            SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(6))
        };

        var game = Game.Create(
            "My Game", "XYZ123", endAt, 6, 20,
            conditionBonuses: bonuses,
            confirmationTimeout: TimeSpan.FromMinutes(10),
            safeTimeBlocks: safeBlocks,
            id: gameId,
            createdAt: now);

        Assert.Equal(gameId, game.Id);
        Assert.Equal("My Game", game.Name);
        Assert.Equal("XYZ123", game.InviteCode);
        Assert.Equal(GameStatus.NotStarted, game.Status);
        Assert.Equal(now, game.CreatedAt);
        Assert.Equal(endAt, game.ScheduledEndAt);
        Assert.Null(game.EndedAt);
        Assert.Equal(6, game.MaxPlayers);
        Assert.Equal(20, game.BasePointsPerTag);
        Assert.Equal(TimeSpan.FromMinutes(10), game.ConfirmationTimeout);
        Assert.Single(game.SafeTimeBlocks);
        Assert.Single(game.ConditionBonuses);
        Assert.Equal(15, game.ConditionBonuses[ConditionType.Alone]);
    }

    [Fact]
    public void Create_NullBonuses_DefaultsToEmptyDictionary()
    {
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10, conditionBonuses: null);
        Assert.NotNull(game.ConditionBonuses);
        Assert.Empty(game.ConditionBonuses);
    }

    [Fact]
    public void Create_NullSafeTimeBlocks_DefaultsToEmptyList()
    {
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10, safeTimeBlocks: null);
        Assert.NotNull(game.SafeTimeBlocks);
        Assert.Empty(game.SafeTimeBlocks);
    }

    [Fact]
    public void Create_NullConfirmationTimeout_DefaultsToFiveMinutes()
    {
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10);
        Assert.Equal(TimeSpan.FromMinutes(5), game.ConfirmationTimeout);
    }

    [Fact]
    public void Create_DefaultId_IsNotEmpty()
    {
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10);
        Assert.NotEqual(Guid.Empty, game.Id);
    }

    [Fact]
    public void Create_DefaultCreatedAt_IsUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10);
        var after = DateTimeOffset.UtcNow;
        Assert.InRange(game.CreatedAt, before, after);
    }

    // ── Start ──────────────────────────────────────────────────────────────

    [Fact]
    public void Start_NotStarted_TransitionsToActive()
    {
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10);
        game.Start();
        Assert.Equal(GameStatus.Active, game.Status);
    }

    [Fact]
    public void Start_AlreadyActive_ThrowsInvalidOperationException()
    {
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10);
        game.Start();

        var ex = Assert.Throws<InvalidOperationException>(() => game.Start());
        Assert.Contains("not started", ex.Message);
    }

    [Fact]
    public void Start_AlreadyEnded_ThrowsInvalidOperationException()
    {
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10);
        game.End();

        var ex = Assert.Throws<InvalidOperationException>(() => game.Start());
        Assert.Contains("not started", ex.Message);
    }

    // ── End ────────────────────────────────────────────────────────────────

    [Fact]
    public void End_NotStarted_SetsEndedAndStatus()
    {
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10);
        var endedAt = DateTimeOffset.UtcNow.AddHours(1);

        game.End(endedAt);

        Assert.Equal(GameStatus.Ended, game.Status);
        Assert.Equal(endedAt, game.EndedAt);
    }

    [Fact]
    public void End_Active_SetsEndedAndStatus()
    {
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10);
        game.Start();
        var endedAt = DateTimeOffset.UtcNow.AddHours(2);

        game.End(endedAt);

        Assert.Equal(GameStatus.Ended, game.Status);
        Assert.Equal(endedAt, game.EndedAt);
    }

    [Fact]
    public void End_AlreadyEnded_IsIdempotent()
    {
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10);
        var endedAt = DateTimeOffset.UtcNow;
        game.End(endedAt);

        game.End(DateTimeOffset.UtcNow.AddHours(1)); // second call

        Assert.Equal(GameStatus.Ended, game.Status);
        Assert.Equal(endedAt, game.EndedAt); // unchanged
    }

    [Fact]
    public void End_NoEndedAtProvided_UsesUtcNow()
    {
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10);
        var before = DateTimeOffset.UtcNow;

        game.End();

        var after = DateTimeOffset.UtcNow;
        Assert.Equal(GameStatus.Ended, game.Status);
        Assert.NotNull(game.EndedAt);
        Assert.InRange(game.EndedAt!.Value, before, after);
    }

    // ── SafeTimeBlocks defensive copy ──────────────────────────────────────

    [Fact]
    public void SafeTimeBlocks_ExternalMutation_DoesNotAffectGame()
    {
        var blocks = new List<SafeTimeBlock>
        {
            SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(6))
        };
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10, safeTimeBlocks: blocks);

        blocks.Clear(); // mutate original list

        Assert.Single(game.SafeTimeBlocks);
    }

    [Fact]
    public void SafeTimeBlocks_GetterReturnsOriginalList_CanMutate()
    {
        var blocks = new List<SafeTimeBlock>
        {
            SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(6))
        };
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10, safeTimeBlocks: blocks);

        var internalList = game.SafeTimeBlocks;
        Assert.Single(internalList);

        // If the consumer clears it, the game's list is affected (same reference).
        // This is intentional — the defensive copy is on input, not output.
        // Verify it's the same reference.
        internalList.Clear();
        Assert.Empty(game.SafeTimeBlocks);
    }

    // ── ConditionBonuses defensive copy ────────────────────────────────────

    [Fact]
    public void ConditionBonuses_ExternalMutation_DoesNotAffectGame()
    {
        var bonuses = new Dictionary<ConditionType, int> { { ConditionType.Alone, 10 } };
        var game = Game.Create("Test", "CODE", DefaultEndAt, 4, 10, conditionBonuses: bonuses);

        bonuses.Clear(); // mutate original

        Assert.Single(game.ConditionBonuses);
    }

    [Fact]
    public void ConditionBonuses_NegativeValueInConstructor_Throws()
    {
        var bonuses = new Dictionary<ConditionType, int> { { ConditionType.Alone, -1 } };
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Game.Create("Test", "CODE", DefaultEndAt, 4, 10, conditionBonuses: bonuses));
        Assert.Contains("conditionBonuses", ex.Message);
    }

    // ── JSON Constructor ───────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithJsonConstructor_SetsStatusToNotStarted()
    {
        var game = new Game(
            Guid.NewGuid(), "Test", "CODE", DateTimeOffset.UtcNow,
            DefaultEndAt, 4, 10, null, TimeSpan.FromMinutes(5), null);
        Assert.Equal(GameStatus.NotStarted, game.Status);
    }
}
