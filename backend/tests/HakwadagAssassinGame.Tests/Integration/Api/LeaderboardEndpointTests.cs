using System.Net;
using System.Net.Http.Json;
using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Tests.Integration.Api;

public sealed class LeaderboardEndpointTests : ApiTestBase
{
    public LeaderboardEndpointTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetLeaderboard_ReturnsPlayersOrderedByScore()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);

        // Add players with different scores
        var p1 = await SeedPlayerAsync("p1@test.com", "Alice");
        var p2 = await SeedPlayerAsync("p2@test.com", "Bob");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, p1.Id));
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, p2.Id));

        // Set scores - leaderboard should order by descending score
        var gp1 = (await GamePlayerRepo.GetAsync(game.Id, p1.Id))!;
        gp1.AddScore(30);
        await GamePlayerRepo.UpdateAsync(gp1);

        var gp2 = (await GamePlayerRepo.GetAsync(game.Id, p2.Id))!;
        gp2.AddScore(50);
        await GamePlayerRepo.UpdateAsync(gp2);

        var token = await CreateTokenAsync(creator);
        var response = await AuthenticatedGetAsync(
            $"/api/games/{game.Id}/leaderboard", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entries = await response.Content.ReadFromJsonAsync<List<LeaderboardEntryDto>>();
        Assert.NotNull(entries);
        Assert.Equal(3, entries.Count);
        Assert.Equal("Bob", entries[0].Player.DisplayName); // 50 points - first
        Assert.Equal("Alice", entries[1].Player.DisplayName); // 30 points - second
        Assert.Equal("Creator", entries[2].Player.DisplayName); // 0 points - third
    }

    [Fact]
    public async Task GetLeaderboard_NonExistentGame_Returns404()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync();

        var response = await AuthenticatedGetAsync(
            $"/api/games/{Guid.NewGuid()}/leaderboard", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
