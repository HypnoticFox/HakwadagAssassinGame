using System.Text.Json.Serialization;

namespace HakwadagAssassinGame.Core.Entities;

/// <summary>Represents a player who can participate in games.</summary>
public sealed class Player
{
    [JsonConstructor]
    public Player(Guid id, string email, string displayName, string? avatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Id = id;
        Email = email;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
    }

    /// <summary>Creates a player.</summary>
    public static Player Create(
        string email,
        string displayName,
        string? avatarUrl = null,
        Guid? id = null) => new(id ?? Guid.NewGuid(), email, displayName, avatarUrl);

    /// <summary>Gets the player identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the player's email address.</summary>
    public string Email { get; private set; }

    /// <summary>Gets the player's display name.</summary>
    public string DisplayName { get; private set; }

    /// <summary>Gets the player's avatar URL, when one exists.</summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>Changes the display name used in a game.</summary>
    public void ChangeDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName;
    }
}
