using System.Net;
using System.Net.Http.Json;
using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Tests.Integration.Api;

public sealed class AssignmentEndpointTests : ApiTestBase
{
    public AssignmentEndpointTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetMyAssignment_ActiveGame_ReturnsAssignment()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync("hunter@test.com", "Hunter");

        // Set up active game with assignment
        var game = await SeedGameAsync(creator: player, status: GameStatus.Active);
        var target = await SeedPlayerAsync("target@test.com", "Target");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, target.Id));

        var condition = AloneCondition.Create();
        var assignment = Assignment.Create(game.Id, player.Id, target.Id, [condition]);
        await AssignmentRepo.AddAsync(assignment);

        var response = await AuthenticatedGetAsync(
            $"/api/games/{game.Id}/assignments/me", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AssignmentDto>();
        Assert.NotNull(dto);
        Assert.Equal(assignment.Id, dto.Id);
        Assert.Equal(target.Id, dto.Target.Id);
        Assert.NotEmpty(dto.Conditions);
    }

    [Fact]
    public async Task GetMyAssignment_NoAssignment_Returns404()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync("player@test.com", "Player");
        var game = await SeedGameAsync(creator: player, status: GameStatus.Active);

        var response = await AuthenticatedGetAsync(
            $"/api/games/{game.Id}/assignments/me", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyAssignment_NotMember_Returns403()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync("player@test.com", "Player");
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);

        var response = await AuthenticatedGetAsync(
            $"/api/games/{game.Id}/assignments/me", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
