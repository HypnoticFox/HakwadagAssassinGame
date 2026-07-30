using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Tests.Unit.Core;

public sealed class GamePlayerTests
{
    private static readonly Guid GameId = Guid.NewGuid();
    private static readonly Guid PlayerId = Guid.NewGuid();

    // ── Create validation ──────────────────────────────────────────────────

    [Fact]
    public void Create_EmptyGameId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            GamePlayer.Create(Guid.Empty, PlayerId));
        Assert.Contains("gameId", ex.Message);
    }

    [Fact]
    public void Create_EmptyPlayerId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            GamePlayer.Create(GameId, Guid.Empty));
        Assert.Contains("playerId", ex.Message);
    }

    [Fact]
    public void Create_ValidInputs_SetsPropertiesCorrectly()
    {
        var gp = GamePlayer.Create(GameId, PlayerId, GameRole.Creator);

        Assert.Equal(GameId, gp.GameId);
        Assert.Equal(PlayerId, gp.PlayerId);
        Assert.Equal(GameRole.Creator, gp.Role);
        Assert.Equal(0, gp.Score);
        Assert.True(gp.IsActive);
    }

    [Fact]
    public void Create_DefaultRoleIsPlayer()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        Assert.Equal(GameRole.Player, gp.Role);
    }

    [Fact]
    public void Create_ScoreDefaultsToZero()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        Assert.Equal(0, gp.Score);
    }

    [Fact]
    public void Create_IsActiveDefaultsToTrue()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        Assert.True(gp.IsActive);
    }

    // ── ResetScore ─────────────────────────────────────────────────────────

    [Fact]
    public void ResetScore_ResetsToZero()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        gp.AddScore(50);
        Assert.Equal(50, gp.Score);

        gp.ResetScore();
        Assert.Equal(0, gp.Score);
    }

    [Fact]
    public void ResetScore_FromZero_StaysZero()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        Assert.Equal(0, gp.Score);

        gp.ResetScore();
        Assert.Equal(0, gp.Score);
    }

    [Fact]
    public void ResetScore_AfterMultipleAdds_ResetsToZero()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        gp.AddScore(10);
        gp.AddScore(20);
        gp.AddScore(30);
        Assert.Equal(60, gp.Score);

        gp.ResetScore();
        Assert.Equal(0, gp.Score);
    }

    // ── AddScore ───────────────────────────────────────────────────────────

    [Fact]
    public void AddScore_Positive_IncreasesScore()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        gp.AddScore(10);
        Assert.Equal(10, gp.Score);
    }

    [Fact]
    public void AddScore_Zero_DoesNotChangeScore()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        gp.AddScore(0);
        Assert.Equal(0, gp.Score);
    }

    [Fact]
    public void AddScore_MultipleCalls_Accumulates()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        gp.AddScore(10);
        gp.AddScore(20);
        gp.AddScore(5);
        Assert.Equal(35, gp.Score);
    }

    [Fact]
    public void AddScore_Negative_ThrowsArgumentOutOfRangeException()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            gp.AddScore(-5));
        Assert.Contains("points", ex.Message);
    }

    // ── RemoveScore ────────────────────────────────────────────────────────

    [Fact]
    public void RemoveScore_Valid_DecreasesScore()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        gp.AddScore(50);
        gp.RemoveScore(30);
        Assert.Equal(20, gp.Score);
    }

    [Fact]
    public void RemoveScore_Zero_DoesNotChangeScore()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        gp.AddScore(10);
        gp.RemoveScore(0);
        Assert.Equal(10, gp.Score);
    }

    [Fact]
    public void RemoveScore_ExactBalance_ResultsInZero()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        gp.AddScore(25);
        gp.RemoveScore(25);
        Assert.Equal(0, gp.Score);
    }

    [Fact]
    public void RemoveScore_Negative_ThrowsArgumentOutOfRangeException()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            gp.RemoveScore(-5));
        Assert.Contains("points", ex.Message);
    }

    [Fact]
    public void RemoveScore_MoreThanScore_ThrowsInvalidOperationException()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        gp.AddScore(10);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            gp.RemoveScore(11));
        Assert.Contains("score cannot become negative", ex.Message);
    }

    [Fact]
    public void RemoveScore_FromZeroScore_ThrowsInvalidOperationException()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            gp.RemoveScore(1));
        Assert.Contains("score cannot become negative", ex.Message);
    }

    // ── Deactivate ─────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        Assert.True(gp.IsActive);

        gp.Deactivate();
        Assert.False(gp.IsActive);
    }

    [Fact]
    public void Deactivate_Twice_RemainsFalse()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        gp.Deactivate();
        gp.Deactivate();
        Assert.False(gp.IsActive);
    }

    // ── PromoteToCoAdmin ───────────────────────────────────────────────────

    [Fact]
    public void PromoteToCoAdmin_Player_ChangesToCoAdmin()
    {
        var gp = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        gp.PromoteToCoAdmin();
        Assert.Equal(GameRole.CoAdmin, gp.Role);
    }

    [Fact]
    public void PromoteToCoAdmin_CoAdmin_StaysCoAdmin()
    {
        var gp = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        gp.PromoteToCoAdmin();
        gp.PromoteToCoAdmin(); // second promotion is a no-op (role stays CoAdmin)
        Assert.Equal(GameRole.CoAdmin, gp.Role);
    }

    [Fact]
    public void PromoteToCoAdmin_Creator_ThrowsInvalidOperationException()
    {
        var gp = GamePlayer.Create(GameId, PlayerId, GameRole.Creator);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            gp.PromoteToCoAdmin());
        Assert.Contains("creator", ex.Message);
    }

    // ── DemoteToPlayer ─────────────────────────────────────────────────────

    [Fact]
    public void DemoteToPlayer_CoAdmin_ChangesToPlayer()
    {
        var gp = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        gp.PromoteToCoAdmin();
        Assert.Equal(GameRole.CoAdmin, gp.Role);

        gp.DemoteToPlayer();
        Assert.Equal(GameRole.Player, gp.Role);
    }

    [Fact]
    public void DemoteToPlayer_Player_StaysPlayer()
    {
        var gp = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        gp.DemoteToPlayer(); // demoting a player is a no-op
        Assert.Equal(GameRole.Player, gp.Role);
    }

    [Fact]
    public void DemoteToPlayer_Creator_ThrowsInvalidOperationException()
    {
        var gp = GamePlayer.Create(GameId, PlayerId, GameRole.Creator);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            gp.DemoteToPlayer());
        Assert.Contains("creator", ex.Message);
    }

    // ── Role promotion/demotion round-trip ─────────────────────────────────

    [Fact]
    public void PromoteThenDemote_PlayerRole_RoundTrips()
    {
        var gp = GamePlayer.Create(GameId, PlayerId, GameRole.Player);

        gp.PromoteToCoAdmin();
        Assert.Equal(GameRole.CoAdmin, gp.Role);

        gp.DemoteToPlayer();
        Assert.Equal(GameRole.Player, gp.Role);
    }

    // ── IsParticipating ────────────────────────────────────────────────────

    [Fact]
    public void Create_IsParticipatingDefaultsToTrue()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        Assert.True(gp.IsParticipating);
    }

    [Fact]
    public void SetParticipating_False_SetsIsParticipatingToFalse()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        Assert.True(gp.IsParticipating);

        gp.SetParticipating(false);
        Assert.False(gp.IsParticipating);
    }

    [Fact]
    public void SetParticipating_True_SetsIsParticipatingBackToTrue()
    {
        var gp = GamePlayer.Create(GameId, PlayerId);
        gp.SetParticipating(false);
        Assert.False(gp.IsParticipating);

        gp.SetParticipating(true);
        Assert.True(gp.IsParticipating);
    }

    // ── JSON Constructor ───────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithJsonConstructor_SetsProperties()
    {
        var gp = new GamePlayer(GameId, PlayerId, GameRole.CoAdmin);

        Assert.Equal(GameId, gp.GameId);
        Assert.Equal(PlayerId, gp.PlayerId);
        Assert.Equal(GameRole.CoAdmin, gp.Role);
        Assert.Equal(0, gp.Score);
        Assert.True(gp.IsActive);
    }
}
