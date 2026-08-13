using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Tests.Integration.Api;

public sealed class GameEndpointTests : ApiTestBase
{
    public GameEndpointTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── Create Game ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateGame_ValidRequest_Returns201WithGame()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync();
        var request = new CreateGameRequest(
            "New Game", 48, 10, 20, 5, null, null);

        var response = await AuthenticatedPostAsync("/api/games", request, token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var game = await response.Content.ReadFromJsonAsync<GameDto>();
        Assert.NotNull(game);
        Assert.Equal("New Game", game.Name);
        Assert.Equal(GameStatus.NotStarted, game.Status);
        Assert.Equal("TESTCD", game.InviteCode);
        Assert.Equal(GameRole.Creator, game.MyRole);
    }

    [Fact]
    public async Task CreateGame_NoDuration_Returns201WithNullScheduledEnd()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync();
        var request = new CreateGameRequest(
            "Open Ended Game", null, 10, 20, 5, null, null);

        var response = await AuthenticatedPostAsync("/api/games", request, token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var game = await response.Content.ReadFromJsonAsync<GameDto>();
        Assert.NotNull(game);
        Assert.Null(game!.ScheduledEndAt);
    }

    [Fact]
    public async Task CreateGame_DurationZero_Returns400()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync();
        var request = new CreateGameRequest(
            "Bad Game", 0, 10, 20, 5, null, null);

        var response = await AuthenticatedPostAsync("/api/games", request, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_NoAuth_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/games",
            new CreateGameRequest("Game", 24, 10, 10, 5, null, null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Get My Games ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyGames_ReturnsPlayersGames()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync();
        var game1 = await SeedGameAsync("Game1", creator: player);
        var game2 = await SeedGameAsync("Game2", "CODE2", creator: player);

        var response = await AuthenticatedGetAsync("/api/games", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var games = await response.Content.ReadFromJsonAsync<List<GameDto>>();
        Assert.NotNull(games);
        Assert.Equal(2, games.Count);
    }

    [Fact]
    public async Task GetMyGames_NoMemberships_ReturnsEmptyList()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync();

        var response = await AuthenticatedGetAsync("/api/games", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var games = await response.Content.ReadFromJsonAsync<List<GameDto>>();
        Assert.NotNull(games);
        Assert.Empty(games);
    }

    // ── Get Game ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetGame_ValidId_ReturnsGame()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync();
        var game = await SeedGameAsync(creator: player);

        var response = await AuthenticatedGetAsync($"/api/games/{game.Id}", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<GameDto>();
        Assert.NotNull(dto);
        Assert.Equal(game.Id, dto.Id);
    }

    [Fact]
    public async Task GetGame_NonExistent_Returns404()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync();

        var response = await AuthenticatedGetAsync($"/api/games/{Guid.NewGuid()}", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetGame_NotMember_Returns401()
    {
        var (owner, _) = await CreateAuthenticatedPlayerAsync("owner@test.com", "Owner");
        var game = await SeedGameAsync(creator: owner);
        var (other, token) = await CreateAuthenticatedPlayerAsync("other@test.com", "Other");

        var response = await AuthenticatedGetAsync($"/api/games/{game.Id}", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Get Players ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetPlayers_AsMember_ReturnsPlayersWithRoles()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        var second = await SeedPlayerAsync("p2@test.com", "Player2");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, second.Id));

        var response = await AuthenticatedGetAsync($"/api/games/{game.Id}/players", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var players = await response.Content.ReadFromJsonAsync<List<GamePlayerDto>>();
        Assert.NotNull(players);
        Assert.Equal(2, players.Count);
        Assert.Equal(GameRole.Creator, players[0].Role);
        Assert.Equal(creator.DisplayName, players[0].DisplayName);
        Assert.Equal(GameRole.Player, players[1].Role);
        Assert.Equal(second.DisplayName, players[1].DisplayName);
    }

    [Fact]
    public async Task GetPlayers_GameNotFound_Returns404()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync();

        var response = await AuthenticatedGetAsync($"/api/games/{Guid.NewGuid()}/players", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPlayers_NoAuth_Returns401()
    {
        var response = await Client.GetAsync($"/api/games/{Guid.NewGuid()}/players");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Join Game ─────────────────────────────────────────────────────────

    [Fact]
    public async Task JoinGame_ValidCode_Returns200()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        var (joiner, token) = await CreateAuthenticatedPlayerAsync("joiner@test.com", "Joiner");

        var response = await AuthenticatedPostAsync(
            $"/api/games/join/{game.InviteCode}",
            new JoinGameRequest("JoinerDisplay"),
            token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<GameDto>();
        Assert.NotNull(dto);
        Assert.Equal(game.Id, dto.Id);
        Assert.Equal(2, dto.PlayerCount);
    }

    [Fact]
    public async Task JoinGame_InvalidCode_Returns404()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync();

        var response = await AuthenticatedPostAsync(
            "/api/games/join/INVALID",
            new JoinGameRequest("Player"),
            token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task JoinGame_StartedGame_Returns400()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator, status: GameStatus.Active);
        var (joiner, token) = await CreateAuthenticatedPlayerAsync("joiner@test.com", "Joiner");

        var response = await AuthenticatedPostAsync(
            $"/api/games/join/{game.InviteCode}",
            new JoinGameRequest("Joiner"),
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task JoinGame_AlreadyMember_Returns400()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync();
        var game = await SeedGameAsync(creator: player);

        var response = await AuthenticatedPostAsync(
            $"/api/games/join/{game.InviteCode}",
            new JoinGameRequest("Again"),
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Start Game ────────────────────────────────────────────────────────

    [Fact]
    public async Task StartGame_AsCreator_WithEnoughPlayers_Returns200()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);

        // Add a second player
        var p2 = await SeedPlayerAsync("p2@test.com", "Player2");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, p2.Id));

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/start", new { }, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<GameDto>();
        Assert.NotNull(dto);
        Assert.Equal(GameStatus.Active, dto!.Status);
    }

    [Fact]
    public async Task StartGame_NotCreator_Returns403()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        var (other, token) = await CreateAuthenticatedPlayerAsync("other@test.com", "Other");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, other.Id));

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/start", new { }, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StartGame_OnlyOnePlayer_Returns400()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        // Only one active player (the creator)

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/start", new { }, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── End Game ──────────────────────────────────────────────────────────

    [Fact]
    public async Task EndGame_AsCreator_Returns200()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator, status: GameStatus.Active);

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/end", new { }, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<GameDto>();
        Assert.NotNull(dto);
        Assert.Equal(GameStatus.Ended, dto!.Status);
    }

    [Fact]
    public async Task EndGame_NotAdmin_Returns403()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator, status: GameStatus.Active);
        var (other, token) = await CreateAuthenticatedPlayerAsync("other@test.com", "Other");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, other.Id));

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/end", new { }, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Leave Game ────────────────────────────────────────────────────────

    [Fact]
    public async Task LeaveGame_AsPlayer_Returns200()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        var (leaver, token) = await CreateAuthenticatedPlayerAsync("leaver@test.com", "Leaver");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, leaver.Id));

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/leave", new { }, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LeaveGame_NotMember_Returns401()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync();

        var response = await AuthenticatedPostAsync(
            $"/api/games/{Guid.NewGuid()}/leave", new { }, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
