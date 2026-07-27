namespace HakwadagAssassinGame.Core.Enums;

/// <summary>Represents a player's role in a game.</summary>
public enum GameRole
{
    /// <summary>A regular player.</summary>
    Player,

    /// <summary>The player who created the game.</summary>
    Creator,

    /// <summary>A player delegated administrative rights.</summary>
    CoAdmin
}
