using HakwadagAssassinGame.Core.Enums;
using System.Text.Json.Serialization;

namespace HakwadagAssassinGame.Core.Entities;

/// <summary>Represents a player's membership and score in a game.</summary>
public sealed class GamePlayer
{
    [JsonConstructor]
    public GamePlayer(Guid gameId, Guid playerId, GameRole role)
    {
        if (gameId == Guid.Empty)
        {
            throw new ArgumentException("A game identifier is required.", nameof(gameId));
        }

        if (playerId == Guid.Empty)
        {
            throw new ArgumentException("A player identifier is required.", nameof(playerId));
        }

        GameId = gameId;
        PlayerId = playerId;
        Role = role;
        Score = 0;
        IsActive = true;
    }

    /// <summary>Creates a game membership.</summary>
    public static GamePlayer Create(Guid gameId, Guid playerId, GameRole role = GameRole.Player) =>
        new(gameId, playerId, role);

    /// <summary>Gets the game identifier.</summary>
    public Guid GameId { get; private set; }

    /// <summary>Gets the player identifier.</summary>
    public Guid PlayerId { get; private set; }

    /// <summary>Gets the player's role in the game.</summary>
    public GameRole Role { get; private set; }

    /// <summary>Gets the player's score.</summary>
    public int Score { get; private set; }

    /// <summary>Gets a value indicating whether the player is still active in the game.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Adds points to the player's score.</summary>
    public void AddScore(int points)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(points);
        Score += points;
    }

    /// <summary>Removes points from the player's score.</summary>
    public void RemoveScore(int points)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(points);
        if (points > Score)
        {
            throw new InvalidOperationException("A score cannot become negative.");
        }

        Score -= points;
    }

    /// <summary>Marks the player as no longer active in the game.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Promotes a regular player to co-admin.</summary>
    public void PromoteToCoAdmin()
    {
        if (Role == GameRole.Creator)
        {
            throw new InvalidOperationException("The creator cannot be changed to co-admin.");
        }

        Role = GameRole.CoAdmin;
    }

    /// <summary>Demotes a co-admin to a regular player.</summary>
    public void DemoteToPlayer()
    {
        if (Role == GameRole.Creator)
        {
            throw new InvalidOperationException("The creator cannot be demoted.");
        }

        Role = GameRole.Player;
    }
}
