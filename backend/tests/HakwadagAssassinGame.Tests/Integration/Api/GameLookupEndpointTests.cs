using System.Net;
using System.Net.Http.Json;
using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Tests.Integration.Api;

public sealed class GameLookupEndpointTests : ApiTestBase
{
    public GameLookupEndpointTests(CustomWebApplicationFactory factory) : base(factory) { }

    private sealed record GameLookupResponse(Guid Id, string Name, GameStatus Status);

    // ── Lookup Game by Invite Code ───────────────────────────────────────

    [Fact]
    public async Task GetGameByInviteCode_ExistingCode_ReturnsGameInfo()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync("InviteGame", "INVITE1", creator: creator);
        var (_, token) = await CreateAuthenticatedPlayerAsync("lookup@test.com", "Lookup");

        var response = await AuthenticatedGetAsync($"/api/games/lookup/{game.InviteCode}", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GameLookupResponse>();
        Assert.NotNull(result);
        Assert.Equal(game.Id, result.Id);
        Assert.Equal("InviteGame", result.Name);
        Assert.Equal(GameStatus.NotStarted, result.Status);
    }

    [Fact]
    public async Task GetGameByInviteCode_ExistingCode_NotMember_StillReturnsGame()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        var (_, token) = await CreateAuthenticatedPlayerAsync("outsider@test.com", "Outsider");

        var response = await AuthenticatedGetAsync($"/api/games/lookup/{game.InviteCode}", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GameLookupResponse>();
        Assert.NotNull(result);
        Assert.Equal(game.Id, result.Id);
    }

    [Fact]
    public async Task GetGameByInviteCode_NonExistentCode_Returns404()
    {
        var (_, token) = await CreateAuthenticatedPlayerAsync();

        var response = await AuthenticatedGetAsync("/api/games/lookup/INVALID", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetGameByInviteCode_NoAuth_Returns401()
    {
        var response = await Client.GetAsync("/api/games/lookup/INVITE1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
