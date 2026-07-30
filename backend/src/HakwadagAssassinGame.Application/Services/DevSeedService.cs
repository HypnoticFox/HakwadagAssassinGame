using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Interfaces;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Interfaces;

namespace HakwadagAssassinGame.Application.Services;

/// <summary>Development-only shortcuts for manual testing. Not available in production.</summary>
public interface IDevSeedService
{
    /// <summary>Creates or finds a player and returns an auth token, bypassing the OTP flow.</summary>
    /// <param name="request">The login request with an optional email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An auth response with token and player details.</returns>
    Task<AuthResponse> DevLoginAsync(DevLoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Creates players, a game, joins all players, starts it, and returns the full state.</summary>
    /// <param name="request">The seed request specifying player count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created game and all seeded players with tokens.</returns>
    Task<SeedGameResponse> SeedGameAsync(SeedGameRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Default dev seed service.</summary>
public sealed class DevSeedService : IDevSeedService
{
    private readonly IPlayerRepository playerRepository;
    private readonly ITokenStore tokenStore;
    private readonly IGameService gameService;

    /// <summary>Initializes the dev seed service.</summary>
    public DevSeedService(
        IPlayerRepository playerRepository,
        ITokenStore tokenStore,
        IGameService gameService)
    {
        this.playerRepository = playerRepository;
        this.tokenStore = tokenStore;
        this.gameService = gameService;
    }

    /// <inheritdoc />
    public async Task<AuthResponse> DevLoginAsync(DevLoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = (request.Email ?? $"dev-{Guid.NewGuid():N}@test.local").Trim();

        var player = await playerRepository.GetByEmailAsync(email, cancellationToken);
        if (player is null)
        {
            player = Player.Create(email, email.Split('@')[0]);
            await playerRepository.AddAsync(player, cancellationToken);
        }

        var token = Guid.NewGuid().ToString("N");
        await tokenStore.StoreAsync(token, player.Id, cancellationToken);
        return new AuthResponse(token, PlayerDto.FromEntity(player));
    }

    /// <inheritdoc />
    public async Task<SeedGameResponse> SeedGameAsync(SeedGameRequest request, CancellationToken cancellationToken = default)
    {
        var count = Math.Clamp(request.PlayerCount ?? 5, 3, 20);

        // Create dev players with predictable emails and auth tokens
        var seededPlayers = new List<(Player Player, string Token)>(count);
        for (var i = 1; i <= count; i++)
        {
            var email = $"player{i}@test.local";
            var displayName = $"Player {i}";
            var player = Player.Create(email, displayName);
            await playerRepository.AddAsync(player, cancellationToken);

            var token = Guid.NewGuid().ToString("N");
            await tokenStore.StoreAsync(token, player.Id, cancellationToken);

            seededPlayers.Add((player, token));
        }

        // Player 1 creates the game (becomes Creator)
        var createRequest = new CreateGameRequest(
            Name: "Dev Test Game",
            DurationHours: 24,
            MaxPlayers: count + 5,
            BasePointsPerTag: 100,
            ConfirmationTimeoutMinutes: 30,
            ConditionBonuses: null,
            SafeTimeBlocks: null);

        var gameDto = await gameService.CreateGameAsync(seededPlayers[0].Player.Id, createRequest, cancellationToken);

        // Players 2..N join via the invite code
        for (var i = 1; i < seededPlayers.Count; i++)
        {
            await gameService.JoinGameAsync(
                seededPlayers[i].Player.Id,
                gameDto.InviteCode,
                new JoinGameRequest(seededPlayers[i].Player.DisplayName),
                cancellationToken);
        }

        // Start the game so assignments are created
        gameDto = await gameService.StartGameAsync(seededPlayers[0].Player.Id, gameDto.Id, cancellationToken);

        var resultPlayers = seededPlayers
            .Select(p => new SeededPlayerDto(PlayerDto.FromEntity(p.Player), p.Token))
            .ToList();

        return new SeedGameResponse(gameDto, resultPlayers);
    }
}
