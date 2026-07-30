using System.Net;
using System.Net.Http.Json;
using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Tests.Integration.Api;

public sealed class TagEndpointTests : ApiTestBase
{
    public TagEndpointTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<(Assignment Assignment, Player Hunter, Player Target, string Token)>
        SetupActiveGameWithAssignmentAsync()
    {
        var hunter = await SeedPlayerAsync("hunter@test.com", "Hunter");
        var target = await SeedPlayerAsync("target@test.com", "Target");
        var token = await CreateTokenAsync(hunter);

        var game = await SeedGameAsync(creator: hunter, status: GameStatus.Active);
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, target.Id));

        var condition = AloneCondition.Create();
        var assignment = Assignment.Create(game.Id, hunter.Id, target.Id, [condition]);
        await AssignmentRepo.AddAsync(assignment);

        return (assignment, hunter, target, token);
    }

    // ── Submit Tag ────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitTag_ValidRequest_Returns201()
    {
        var (assignment, hunter, target, token) = await SetupActiveGameWithAssignmentAsync();
        var conditionId = assignment.Conditions[0].Id;

        var response = await AuthenticatedPostAsync(
            $"/api/games/{assignment.GameId}/tag",
            new SubmitTagRequest(assignment.Id, conditionId),
            token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await Factory.MockPushService.Received(1)
            .SendNotificationAsync(target.Id,
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitTag_WrongHunter_Returns403()
    {
        var (assignment, _, _, _) = await SetupActiveGameWithAssignmentAsync();
        var (imposter, token) = await CreateAuthenticatedPlayerAsync("imposter@test.com", "Imposter");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(assignment.GameId, imposter.Id));
        var conditionId = assignment.Conditions[0].Id;

        var response = await AuthenticatedPostAsync(
            $"/api/games/{assignment.GameId}/tag",
            new SubmitTagRequest(assignment.Id, conditionId),
            token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubmitTag_NonExistentCondition_Returns400()
    {
        var (assignment, _, _, token) = await SetupActiveGameWithAssignmentAsync();

        var response = await AuthenticatedPostAsync(
            $"/api/games/{assignment.GameId}/tag",
            new SubmitTagRequest(assignment.Id, Guid.NewGuid()),
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Get Pending Tag ───────────────────────────────────────────────────

    [Fact]
    public async Task GetPendingTag_TargetHasPending_Returns200()
    {
        var (assignment, hunter, target, _) = await SetupActiveGameWithAssignmentAsync();
        var targetToken = await CreateTokenAsync(target);
        var conditionId = assignment.Conditions[0].Id;

        // Submit a tag
        var submission = TagSubmission.Create(assignment.Id, hunter.Id, target.Id, conditionId);
        await TagRepo.AddAsync(submission);

        var response = await AuthenticatedGetAsync(
            $"/api/games/{assignment.GameId}/tag/pending", targetToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tag = await response.Content.ReadFromJsonAsync<TagSubmissionDto>();
        Assert.NotNull(tag);
        Assert.Equal(submission.Id, tag.Id);
        Assert.Equal(TagStatus.Pending, tag.Status);
    }

    [Fact]
    public async Task GetPendingTag_NoPending_Returns404()
    {
        var (assignment, _, _, _) = await SetupActiveGameWithAssignmentAsync();
        var (player, token) = await CreateAuthenticatedPlayerAsync("other@test.com", "Other");
        await GamePlayerRepo.AddAsync(GamePlayer.Create(assignment.GameId, player.Id));

        var response = await AuthenticatedGetAsync(
            $"/api/games/{assignment.GameId}/tag/pending", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Confirm Tag ───────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmTag_AsTarget_Returns200()
    {
        var (assignment, hunter, target, _) = await SetupActiveGameWithAssignmentAsync();
        var targetToken = await CreateTokenAsync(target);
        var conditionId = assignment.Conditions[0].Id;

        var submission = TagSubmission.Create(assignment.Id, hunter.Id, target.Id, conditionId);
        await TagRepo.AddAsync(submission);

        var response = await AuthenticatedPostAsync(
            $"/api/games/{assignment.GameId}/tag/{submission.Id}/confirm",
            new { }, targetToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tag = await response.Content.ReadFromJsonAsync<TagSubmissionDto>();
        Assert.NotNull(tag);
        Assert.Equal(TagStatus.Confirmed, tag!.Status);
    }

    [Fact]
    public async Task ConfirmTag_WrongPlayer_Returns403()
    {
        var (assignment, hunter, _, _) = await SetupActiveGameWithAssignmentAsync();
        var conditionId = assignment.Conditions[0].Id;

        var submission = TagSubmission.Create(assignment.Id, hunter.Id, Guid.NewGuid(), conditionId);
        await TagRepo.AddAsync(submission);

        // Hunter tries to confirm their own tag (should fail - only target can confirm)
        var response = await AuthenticatedPostAsync(
            $"/api/games/{assignment.GameId}/tag/{submission.Id}/confirm",
            new { }, await CreateTokenAsync(hunter));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Deny Tag ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DenyTag_AsTarget_Returns200()
    {
        var (assignment, hunter, target, _) = await SetupActiveGameWithAssignmentAsync();
        var targetToken = await CreateTokenAsync(target);
        var conditionId = assignment.Conditions[0].Id;

        var submission = TagSubmission.Create(assignment.Id, hunter.Id, target.Id, conditionId);
        await TagRepo.AddAsync(submission);

        var response = await AuthenticatedPostAsync(
            $"/api/games/{assignment.GameId}/tag/{submission.Id}/deny",
            new { }, targetToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tag = await response.Content.ReadFromJsonAsync<TagSubmissionDto>();
        Assert.NotNull(tag);
        Assert.Equal(TagStatus.Denied, tag!.Status);
    }

    // ── Void Tag ──────────────────────────────────────────────────────────

    [Fact]
    public async Task VoidTag_AsAdmin_Returns200()
    {
        var (assignment, hunter, target, _) = await SetupActiveGameWithAssignmentAsync();
        var conditionId = assignment.Conditions[0].Id;

        // Creator is admin
        var (admin, adminToken) = await CreateAuthenticatedPlayerAsync("admin@test.com", "Admin");
        // Re-seed with admin as creator
        var adminGame = await SeedGameAsync("AdminGame", "ADMIN", creator: admin, status: GameStatus.Active);
        await GamePlayerRepo.AddAsync(GamePlayer.Create(adminGame.Id, hunter.Id));
        await GamePlayerRepo.AddAsync(GamePlayer.Create(adminGame.Id, target.Id));

        var adminAssignment = Assignment.Create(adminGame.Id, hunter.Id, target.Id, [conditionId != Guid.Empty ? AloneCondition.Create() : AloneCondition.Create()]);
        // Use a fresh condition
        var freshCondition = AloneCondition.Create();
        var freshAssignment = Assignment.Create(adminGame.Id, hunter.Id, target.Id, [freshCondition]);
        await AssignmentRepo.AddAsync(freshAssignment);

        var submission = TagSubmission.Create(freshAssignment.Id, hunter.Id, target.Id, freshCondition.Id);
        await TagRepo.AddAsync(submission);

        var response = await AuthenticatedPostAsync(
            $"/api/games/{adminGame.Id}/tag/{submission.Id}/void",
            new { }, adminToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tag = await response.Content.ReadFromJsonAsync<TagSubmissionDto>();
        Assert.NotNull(tag);
        Assert.Equal(TagStatus.Voided, tag!.Status);
    }

    [Fact]
    public async Task VoidTag_NotAdmin_Returns403()
    {
        var (assignment, hunter, target, token) = await SetupActiveGameWithAssignmentAsync();
        var conditionId = assignment.Conditions[0].Id;

        var submission = TagSubmission.Create(assignment.Id, hunter.Id, target.Id, conditionId);
        await TagRepo.AddAsync(submission);

        // Hunter (not admin) tries to void
        var response = await AuthenticatedPostAsync(
            $"/api/games/{assignment.GameId}/tag/{submission.Id}/void",
            new { }, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
