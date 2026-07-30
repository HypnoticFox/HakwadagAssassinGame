namespace HakwadagAssassinGame.Application.Dtos;

/// <summary>Request for the development-only auth bypass endpoint.</summary>
/// <param name="Email">Optional email address; a random dev address is used when omitted.</param>
public record DevLoginRequest(string? Email);

/// <summary>Request for the development-only seed-game endpoint.</summary>
/// <param name="PlayerCount">Number of players to seed (default 5, min 3, max 20).</param>
public record SeedGameRequest(int? PlayerCount);

/// <summary>A seeded player with their authentication token.</summary>
/// <param name="Player">The player details.</param>
/// <param name="Token">The authentication token.</param>
public record SeededPlayerDto(PlayerDto Player, string Token);

/// <summary>Response returned after seeding a game for testing.</summary>
/// <param name="Game">The created game.</param>
/// <param name="Players">The seeded players with their tokens.</param>
public record SeedGameResponse(GameDto Game, List<SeededPlayerDto> Players);
