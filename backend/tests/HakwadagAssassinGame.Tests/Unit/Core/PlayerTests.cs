using HakwadagAssassinGame.Core.Entities;

namespace HakwadagAssassinGame.Tests.Unit.Core;

public sealed class PlayerTests
{
    // ── Create validation ──────────────────────────────────────────────────

    [Fact]
    public void Create_NullEmail_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Player.Create(null!, "PlayerOne"));
        Assert.Contains("email", ex.Message);
    }

    [Fact]
    public void Create_EmptyEmail_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Player.Create("", "PlayerOne"));
        Assert.Contains("email", ex.Message);
    }

    [Fact]
    public void Create_WhitespaceEmail_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Player.Create("   ", "PlayerOne"));
        Assert.Contains("email", ex.Message);
    }

    [Fact]
    public void Create_NullDisplayName_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Player.Create("player@test.com", null!));
        Assert.Contains("displayName", ex.Message);
    }

    [Fact]
    public void Create_EmptyDisplayName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Player.Create("player@test.com", ""));
        Assert.Contains("displayName", ex.Message);
    }

    [Fact]
    public void Create_WhitespaceDisplayName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Player.Create("player@test.com", "   "));
        Assert.Contains("displayName", ex.Message);
    }

    [Fact]
    public void Create_ValidInputs_SetsPropertiesCorrectly()
    {
        var playerId = Guid.NewGuid();
        var player = Player.Create("john@example.com", "John", "https://avatar.url/john.png", playerId);

        Assert.Equal(playerId, player.Id);
        Assert.Equal("john@example.com", player.Email);
        Assert.Equal("John", player.DisplayName);
        Assert.Equal("https://avatar.url/john.png", player.AvatarUrl);
    }

    [Fact]
    public void Create_NoAvatarUrl_SetsAvatarUrlToNull()
    {
        var player = Player.Create("john@example.com", "John");
        Assert.Null(player.AvatarUrl);
    }

    [Fact]
    public void Create_DefaultId_IsNotEmpty()
    {
        var player = Player.Create("john@example.com", "John");
        Assert.NotEqual(Guid.Empty, player.Id);
    }

    // ── ChangeDisplayName ──────────────────────────────────────────────────

    [Fact]
    public void ChangeDisplayName_ValidName_UpdatesDisplayName()
    {
        var player = Player.Create("john@example.com", "John");
        player.ChangeDisplayName("Johnny");

        Assert.Equal("Johnny", player.DisplayName);
    }

    [Fact]
    public void ChangeDisplayName_Null_ThrowsArgumentNullException()
    {
        var player = Player.Create("john@example.com", "John");
        var ex = Assert.Throws<ArgumentNullException>(() =>
            player.ChangeDisplayName(null!));
        Assert.Contains("displayName", ex.Message);
    }

    [Fact]
    public void ChangeDisplayName_Empty_ThrowsArgumentException()
    {
        var player = Player.Create("john@example.com", "John");
        var ex = Assert.Throws<ArgumentException>(() =>
            player.ChangeDisplayName(""));
        Assert.Contains("displayName", ex.Message);
    }

    [Fact]
    public void ChangeDisplayName_Whitespace_ThrowsArgumentException()
    {
        var player = Player.Create("john@example.com", "John");
        var ex = Assert.Throws<ArgumentException>(() =>
            player.ChangeDisplayName("   "));
        Assert.Contains("displayName", ex.Message);
    }

    // ── JSON Constructor ───────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithJsonConstructor_SetsProperties()
    {
        var id = Guid.NewGuid();
        var player = new Player(id, "jane@test.com", "Jane", null);

        Assert.Equal(id, player.Id);
        Assert.Equal("jane@test.com", player.Email);
        Assert.Equal("Jane", player.DisplayName);
        Assert.Null(player.AvatarUrl);
    }
}
