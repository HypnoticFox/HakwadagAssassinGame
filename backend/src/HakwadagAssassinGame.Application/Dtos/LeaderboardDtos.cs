namespace HakwadagAssassinGame.Application.Dtos;

/// <summary>Represents a player's position on the leaderboard.</summary>
public record LeaderboardEntryDto(PlayerDto Player, int Score, int Tags);
