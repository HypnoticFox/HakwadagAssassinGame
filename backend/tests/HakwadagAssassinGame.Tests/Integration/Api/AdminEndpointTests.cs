using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Tests.Integration.Api;

public sealed class AdminEndpointTests : ApiTestBase
{
    public AdminEndpointTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── Add Co-Admin ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddCoAdmin_AsCreator_Returns200()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        var target = await SeedPlayerAsync("target@test.com", "Target");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, target.Id));

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/admins",
            new AddAdminRequest(target.Id),
            token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the target was promoted
        var gp = await GamePlayerRepo.GetAsync(game.Id, target.Id);
        Assert.NotNull(gp);
        Assert.Equal(GameRole.CoAdmin, gp!.Role);
    }

    [Fact]
    public async Task AddCoAdmin_NotCreator_Returns403()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        var (other, token) = await CreateAuthenticatedPlayerAsync("other@test.com", "Other");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, other.Id));
        var target = await SeedPlayerAsync("target@test.com", "Target");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, target.Id));

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/admins",
            new AddAdminRequest(target.Id),
            token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Remove Co-Admin ──────────────────────────────────────────────────

    [Fact]
    public async Task RemoveCoAdmin_AsCreator_Returns200()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        var target = await SeedPlayerAsync("target@test.com", "Target");
        var gp = GamePlayer.Create(game.Id, target.Id, GameRole.CoAdmin);
        await GamePlayerRepo.AddAsync(gp);

        var response = await AuthenticatedDeleteAsync(
            $"/api/games/{game.Id}/admins/{target.Id}",
            token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify demotion
        var updated = await GamePlayerRepo.GetAsync(game.Id, target.Id);
        Assert.NotNull(updated);
        Assert.Equal(GameRole.Player, updated!.Role);
    }

    [Fact]
    public async Task RemoveCoAdmin_NotCreator_Returns403()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        var (other, token) = await CreateAuthenticatedPlayerAsync("other@test.com", "Other");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, other.Id));
        var target = await SeedPlayerAsync("target@test.com", "Target");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, target.Id, GameRole.CoAdmin));

        var response = await AuthenticatedDeleteAsync(
            $"/api/games/{game.Id}/admins/{target.Id}",
            token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Add Safe Time Block ──────────────────────────────────────────────

    [Fact]
    public async Task AddSafeTimeBlock_AsAdmin_Returns201()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/safe-times",
            new AddSafeTimeBlockRequest(
                TimeSpan.FromHours(22),
                TimeSpan.FromHours(6),
                null),
            token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("blockId", out var blockIdProp));
        Assert.NotEqual(Guid.Empty, blockIdProp.GetGuid());
    }

    [Fact]
    public async Task AddSafeTimeBlock_NotAdmin_Returns403()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        var (player, token) = await CreateAuthenticatedPlayerAsync("player@test.com", "Player");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, player.Id));

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/safe-times",
            new AddSafeTimeBlockRequest(
                TimeSpan.FromHours(22),
                TimeSpan.FromHours(6),
                null),
            token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Remove Safe Time Block ───────────────────────────────────────────

    [Fact]
    public async Task RemoveSafeTimeBlock_AsAdmin_Returns200()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        var block = SafeTimeBlock.Create(TimeSpan.FromHours(22), TimeSpan.FromHours(6));
        game.SafeTimeBlocks.Add(block);
        await GameRepo.UpdateAsync(game);

        var response = await AuthenticatedDeleteAsync(
            $"/api/games/{game.Id}/safe-times/{block.Id}",
            token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify removal
        var updated = await GameRepo.GetByIdAsync(game.Id);
        Assert.NotNull(updated);
        Assert.DoesNotContain(updated!.SafeTimeBlocks, b => b.Id == block.Id);
    }

    [Fact]
    public async Task RemoveSafeTimeBlock_NotFound_Returns400()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);

        var response = await AuthenticatedDeleteAsync(
            $"/api/games/{game.Id}/safe-times/{Guid.NewGuid()}",
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Add Custom Condition ─────────────────────────────────────────────

    [Fact]
    public async Task AddCustomCondition_AsAdmin_Returns201()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/conditions",
            new AddCustomConditionRequest("Must be holding a banana"),
            token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddCustomCondition_NotAdmin_Returns403()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        var (player, token) = await CreateAuthenticatedPlayerAsync("player@test.com", "Player");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, player.Id));

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/conditions",
            new AddCustomConditionRequest("Must be holding a banana"),
            token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Update Duration ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDuration_AsCreator_Returns200()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);

        var response = await AuthenticatedPutAsync(
            $"/api/games/{game.Id}/duration",
            new UpdateDurationRequest(48),
            token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the scheduled end was updated
        var updated = await GameRepo.GetByIdAsync(game.Id);
        Assert.NotNull(updated);
        Assert.NotNull(updated!.ScheduledEndAt);
        Assert.InRange(
            updated.ScheduledEndAt!.Value,
            DateTimeOffset.UtcNow.AddHours(47),
            DateTimeOffset.UtcNow.AddHours(49));
    }

    [Fact]
    public async Task UpdateDuration_NotCreator_Returns401()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        var (player, token) = await CreateAuthenticatedPlayerAsync("player@test.com", "Player");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, player.Id));

        var response = await AuthenticatedPutAsync(
            $"/api/games/{game.Id}/duration",
            new UpdateDurationRequest(48),
            token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDuration_ActiveGame_Returns400()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        game.Start();
        await GameRepo.UpdateAsync(game);

        var response = await AuthenticatedPutAsync(
            $"/api/games/{game.Id}/duration",
            new UpdateDurationRequest(48),
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Extend Duration ───────────────────────────────────────────────────

    [Fact]
    public async Task ExtendDuration_AsCreator_Returns200()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        game.Start();
        await GameRepo.UpdateAsync(game);
        var originalEnd = game.ScheduledEndAt!.Value;

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/duration/extend",
            new ExtendDurationRequest(60),
            token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the scheduled end was extended by one hour
        var updated = await GameRepo.GetByIdAsync(game.Id);
        Assert.NotNull(updated);
        Assert.Equal(originalEnd.AddHours(1), updated!.ScheduledEndAt);
    }

    [Fact]
    public async Task ExtendDuration_NotCreator_Returns401()
    {
        var (creator, _) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);
        game.Start();
        await GameRepo.UpdateAsync(game);
        var (player, token) = await CreateAuthenticatedPlayerAsync("player@test.com", "Player");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, player.Id));

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/duration/extend",
            new ExtendDurationRequest(60),
            token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExtendDuration_NotStartedGame_Returns400()
    {
        var (creator, token) = await CreateAuthenticatedPlayerAsync("creator@test.com", "Creator");
        var game = await SeedGameAsync(creator: creator);

        var response = await AuthenticatedPostAsync(
            $"/api/games/{game.Id}/duration/extend",
            new ExtendDurationRequest(60),
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
