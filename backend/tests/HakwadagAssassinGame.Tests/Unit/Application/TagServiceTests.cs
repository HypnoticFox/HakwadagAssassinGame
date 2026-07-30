using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;
using NSubstitute;

namespace HakwadagAssassinGame.Tests.Unit.Application;

public sealed class TagServiceTests
{
    private readonly ITagSubmissionRepository tagRepository = Substitute.For<ITagSubmissionRepository>();
    private readonly IAssignmentRepository assignmentRepository = Substitute.For<IAssignmentRepository>();
    private readonly IGameRepository gameRepository = Substitute.For<IGameRepository>();
    private readonly IGamePlayerRepository gamePlayerRepository = Substitute.For<IGamePlayerRepository>();
    private readonly IPlayerRepository playerRepository = Substitute.For<IPlayerRepository>();
    private readonly IPushNotificationService pushNotificationService = Substitute.For<IPushNotificationService>();
    private readonly IConditionLibrary conditionLibrary = Substitute.For<IConditionLibrary>();
    private readonly TagService sut;

    private static readonly Guid HunterId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid GameId = Guid.NewGuid();
    private static readonly Guid ConditionId = Guid.NewGuid();
    private static readonly Guid AssignmentId = Guid.NewGuid();

    public TagServiceTests()
    {
        sut = new TagService(
            tagRepository, assignmentRepository, gameRepository,
            gamePlayerRepository, playerRepository,
            pushNotificationService, conditionLibrary);
    }

    private static Game CreateActiveGame() =>
        Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            new Dictionary<ConditionType, int> { { ConditionType.Alone, 15 } },
            TimeSpan.FromMinutes(15));

    private static Assignment CreateActiveAssignment()
    {
        var condition = AloneCondition.Create();
        // Set a known condition ID via the base constructor
        var assignment = Assignment.Create(
            GameId, HunterId, TargetId,
            new List<Condition> { condition },
            id: AssignmentId);
        return assignment;
    }

    // ── SubmitTagAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitTagAsync_Valid_SubmitsAndReturnsDto()
    {
        var game = CreateActiveGame();
        game.Start();
        // Create assignment with known condition
        var aloneCondition = AloneCondition.Create();
        var assignment = Assignment.Create(GameId, HunterId, TargetId,
            new List<Condition> { aloneCondition }, id: AssignmentId);

        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);
        tagRepository.GetPendingByTargetIdAsync(TargetId, Arg.Any<CancellationToken>())
            .Returns(new List<TagSubmission>());

        var request = new SubmitTagRequest(AssignmentId, assignment.Conditions[0].Id);
        var result = await sut.SubmitTagAsync(HunterId, request);

        Assert.NotNull(result);
        Assert.Equal(TagStatus.Pending, result.Status);
        await tagRepository.Received(1).AddAsync(Arg.Any<TagSubmission>(), Arg.Any<CancellationToken>());
        await pushNotificationService.Received(1).SendNotificationAsync(
            TargetId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitTagAsync_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.SubmitTagAsync(HunterId, null!));
    }

    [Fact]
    public async Task SubmitTagAsync_AssignmentNotFound_ThrowsAssignmentNotFoundException()
    {
        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>())
            .Returns((Assignment?)null);

        await Assert.ThrowsAsync<AssignmentNotFoundException>(() =>
            sut.SubmitTagAsync(HunterId, new SubmitTagRequest(AssignmentId, ConditionId)));
    }

    [Fact]
    public async Task SubmitTagAsync_NotHunter_ThrowsUnauthorizedException()
    {
        var assignment = CreateActiveAssignment();
        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.SubmitTagAsync(TargetId, new SubmitTagRequest(AssignmentId, ConditionId)));
    }

    [Fact]
    public async Task SubmitTagAsync_AssignmentCompleted_ThrowsUnauthorizedException()
    {
        var assignment = CreateActiveAssignment();
        assignment.Complete();
        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.SubmitTagAsync(HunterId, new SubmitTagRequest(AssignmentId, ConditionId)));
    }

    [Fact]
    public async Task SubmitTagAsync_GameNotActive_ThrowsInvalidGameStateException()
    {
        var game = CreateActiveGame(); // NotStarted, not Active
        var assignment = CreateActiveAssignment();

        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.SubmitTagAsync(HunterId, new SubmitTagRequest(AssignmentId, assignment.Conditions[0].Id)));
    }

    [Fact]
    public async Task SubmitTagAsync_ConditionNotInAssignment_ThrowsInvalidGameStateException()
    {
        var game = CreateActiveGame();
        game.Start();
        var assignment = CreateActiveAssignment();

        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        var unknownConditionId = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.SubmitTagAsync(HunterId, new SubmitTagRequest(AssignmentId, unknownConditionId)));
    }

    [Fact]
    public async Task SubmitTagAsync_InSafeTime_ThrowsSafeTimeBlockViolationException()
    {
        // Create a game with an always-active safe time block
        var game = CreateActiveGame();
        game.SafeTimeBlocks.Add(
            SafeTimeBlock.Create(
                TimeSpan.FromHours(0),
                TimeSpan.FromHours(23.9999)));
        game.Start();
        var assignment = CreateActiveAssignment();

        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await Assert.ThrowsAsync<SafeTimeBlockViolationException>(() =>
            sut.SubmitTagAsync(HunterId, new SubmitTagRequest(AssignmentId, assignment.Conditions[0].Id)));
    }

    [Fact]
    public async Task SubmitTagAsync_PendingTagExists_ThrowsPendingTagExistsException()
    {
        var aloneCondition = AloneCondition.Create();
        var assignment = Assignment.Create(GameId, HunterId, TargetId,
            new List<Condition> { aloneCondition }, id: AssignmentId);
        var game = CreateActiveGame();
        game.Start();

        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);
        tagRepository.GetPendingByTargetIdAsync(TargetId, Arg.Any<CancellationToken>())
            .Returns(new List<TagSubmission>
            {
                TagSubmission.Create(AssignmentId, HunterId, TargetId, Guid.NewGuid())
            });

        await Assert.ThrowsAsync<PendingTagExistsException>(() =>
            sut.SubmitTagAsync(HunterId, new SubmitTagRequest(AssignmentId, assignment.Conditions[0].Id)));
    }

    // ── ConfirmTagAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmTagAsync_Valid_ConfirmsAndCreatesReplacement()
    {
        var game = CreateActiveGame();
        game.Start();
        // Override game ID to match GameId constant
        typeof(Game).GetProperty(nameof(Game.Id))!.SetValue(game, GameId);
        var aloneCondition = AloneCondition.Create();
        var assignment = Assignment.Create(GameId, HunterId, TargetId,
            new List<Condition> { aloneCondition }, id: AssignmentId);

        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, aloneCondition.Id);

        tagRepository.GetByIdAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);
        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        var hunterMembership = GamePlayer.Create(GameId, HunterId, GameRole.Player);
        var thirdPlayerId = Guid.NewGuid();
        gamePlayerRepository.GetAsync(GameId, HunterId, Arg.Any<CancellationToken>()).Returns(hunterMembership);
        gamePlayerRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer>
            {
                hunterMembership,
                GamePlayer.Create(GameId, TargetId, GameRole.Player),
                GamePlayer.Create(GameId, thirdPlayerId, GameRole.Player)
            });

        conditionLibrary.GetAsync(GameId, Arg.Any<CancellationToken>())
            .Returns(new List<Condition>());
        playerRepository.GetByIdAsync(HunterId, Arg.Any<CancellationToken>())
            .Returns(Player.Create("hunter@test.com", "Hunter", id: HunterId));
        playerRepository.GetByIdAsync(TargetId, Arg.Any<CancellationToken>())
            .Returns(Player.Create("target@test.com", "Target", id: TargetId));
        playerRepository.GetByIdAsync(thirdPlayerId, Arg.Any<CancellationToken>())
            .Returns(Player.Create("third@test.com", "Third", id: thirdPlayerId));

        var result = await sut.ConfirmTagAsync(TargetId, submission.Id);

        Assert.NotNull(result);
        Assert.Equal(TagStatus.Confirmed, result.Status);
        await tagRepository.Received(1).UpdateAsync(submission, Arg.Any<CancellationToken>());
        await assignmentRepository.Received(1).UpdateAsync(assignment, Arg.Any<CancellationToken>());
        await gamePlayerRepository.Received(1).UpdateAsync(hunterMembership, Arg.Any<CancellationToken>());
        Assert.Equal(115, hunterMembership.Score); // base 100 + 15 condition bonus for Alone
    }

    [Fact]
    public async Task ConfirmTagAsync_NotTarget_ThrowsUnauthorizedException()
    {
        var aloneCondition = AloneCondition.Create();
        var assignment = Assignment.Create(GameId, HunterId, TargetId,
            new List<Condition> { aloneCondition }, id: AssignmentId);
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, aloneCondition.Id);

        tagRepository.GetByIdAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.ConfirmTagAsync(HunterId, submission.Id));
    }

    [Fact]
    public async Task ConfirmTagAsync_NotPending_ThrowsInvalidGameStateException()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, Guid.NewGuid());
        submission.Confirm(); // already confirmed

        tagRepository.GetByIdAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.ConfirmTagAsync(TargetId, submission.Id));
    }

    [Fact]
    public async Task ConfirmTagAsync_SubmissionNotFound_ThrowsTagSubmissionNotFoundException()
    {
        var tagId = Guid.NewGuid();
        tagRepository.GetByIdAsync(tagId, Arg.Any<CancellationToken>()).Returns((TagSubmission?)null);

        await Assert.ThrowsAsync<TagSubmissionNotFoundException>(() =>
            sut.ConfirmTagAsync(TargetId, tagId));
    }

    // ── DenyTagAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DenyTagAsync_Valid_DeniesTag()
    {
        var aloneCondition = AloneCondition.Create();
        var assignment = Assignment.Create(GameId, HunterId, TargetId,
            new List<Condition> { aloneCondition }, id: AssignmentId);
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, aloneCondition.Id);

        tagRepository.GetByIdAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);

        var result = await sut.DenyTagAsync(TargetId, submission.Id);

        Assert.NotNull(result);
        Assert.Equal(TagStatus.Denied, result.Status);
        await tagRepository.Received(1).UpdateAsync(submission, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DenyTagAsync_NotTarget_ThrowsUnauthorizedException()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, Guid.NewGuid());
        tagRepository.GetByIdAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.DenyTagAsync(HunterId, submission.Id));
    }

    [Fact]
    public async Task DenyTagAsync_NotPending_ThrowsInvalidGameStateException()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, Guid.NewGuid());
        submission.Confirm();

        tagRepository.GetByIdAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.DenyTagAsync(TargetId, submission.Id));
    }

    // ── VoidTagAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task VoidTagAsync_AdminVoidsPending_Voids()
    {
        var aloneCondition = AloneCondition.Create();
        var assignment = Assignment.Create(GameId, HunterId, TargetId,
            new List<Condition> { aloneCondition }, id: AssignmentId);
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, aloneCondition.Id);

        var adminMembership = GamePlayer.Create(GameId, AdminId, GameRole.CoAdmin);

        tagRepository.GetByIdAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);
        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        gamePlayerRepository.GetAsync(GameId, AdminId, Arg.Any<CancellationToken>()).Returns(adminMembership);

        var result = await sut.VoidTagAsync(AdminId, submission.Id);

        Assert.NotNull(result);
        Assert.Equal(TagStatus.Voided, result.Status);
        await tagRepository.Received(1).UpdateAsync(submission, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VoidTagAsync_AdminVoidsConfirmed_ReversesScore()
    {
        var game = CreateActiveGame();
        game.Start();
        typeof(Game).GetProperty(nameof(Game.Id))!.SetValue(game, GameId);
        var aloneCondition = AloneCondition.Create();
        var assignment = Assignment.Create(GameId, HunterId, TargetId,
            new List<Condition> { aloneCondition }, id: AssignmentId);
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, aloneCondition.Id);
        submission.Confirm();

        var adminMembership = GamePlayer.Create(GameId, AdminId, GameRole.Creator);
        var hunterMembership = GamePlayer.Create(GameId, HunterId, GameRole.Player);
        // Score is base 100 + alone bonus 15 = 115
        hunterMembership.AddScore(115);

        tagRepository.GetByIdAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);
        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(GameId, AdminId, Arg.Any<CancellationToken>()).Returns(adminMembership);
        gamePlayerRepository.GetAsync(GameId, HunterId, Arg.Any<CancellationToken>()).Returns(hunterMembership);

        // Score is reversed before Void() throws on confirmed tag (domain entity limitation)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.VoidTagAsync(AdminId, submission.Id));
        Assert.Equal(0, hunterMembership.Score); // score was reversed
        await gamePlayerRepository.Received(1).UpdateAsync(hunterMembership, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VoidTagAsync_NotAdmin_ThrowsUnauthorizedException()
    {
        var assignment = CreateActiveAssignment();
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, Guid.NewGuid());
        var playerMembership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);

        tagRepository.GetByIdAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);
        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(playerMembership);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.VoidTagAsync(PlayerId, submission.Id));
    }

    private static readonly Guid PlayerId = Guid.NewGuid();

    [Fact]
    public async Task VoidTagAsync_AlreadyVoided_ThrowsInvalidGameStateException()
    {
        var assignment = CreateActiveAssignment();
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, Guid.NewGuid()); // Pending

        var adminMembership = GamePlayer.Create(GameId, AdminId, GameRole.Creator);

        tagRepository.GetByIdAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);
        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        gamePlayerRepository.GetAsync(GameId, AdminId, Arg.Any<CancellationToken>()).Returns(adminMembership);

        // First void works (pending → voided)
        await sut.VoidTagAsync(AdminId, submission.Id);

        // Tag is now Voided - second call hits the else branch
        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.VoidTagAsync(AdminId, submission.Id));
    }

    [Fact]
    public async Task VoidTagAsync_DeniedTag_ThrowsInvalidGameStateException()
    {
        var aloneCondition = AloneCondition.Create();
        var assignment = Assignment.Create(GameId, HunterId, TargetId,
            new List<Condition> { aloneCondition }, id: AssignmentId);
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, aloneCondition.Id);
        submission.Deny();

        var adminMembership = GamePlayer.Create(GameId, AdminId, GameRole.Creator);

        tagRepository.GetByIdAsync(submission.Id, Arg.Any<CancellationToken>()).Returns(submission);
        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);
        gamePlayerRepository.GetAsync(GameId, AdminId, Arg.Any<CancellationToken>()).Returns(adminMembership);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.VoidTagAsync(AdminId, submission.Id));
    }

    // ── GetPendingTagAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetPendingTagAsync_Valid_ReturnsDto()
    {
        var aloneCondition = AloneCondition.Create();
        var assignment = Assignment.Create(GameId, HunterId, TargetId,
            new List<Condition> { aloneCondition }, id: AssignmentId);
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, aloneCondition.Id);

        gamePlayerRepository.GetAsync(GameId, TargetId, Arg.Any<CancellationToken>())
            .Returns(GamePlayer.Create(GameId, TargetId, GameRole.Player));
        tagRepository.GetPendingByTargetIdAsync(TargetId, Arg.Any<CancellationToken>())
            .Returns(new List<TagSubmission> { submission });
        assignmentRepository.GetByIdAsync(AssignmentId, Arg.Any<CancellationToken>()).Returns(assignment);

        var result = await sut.GetPendingTagAsync(TargetId, GameId);

        Assert.NotNull(result);
        Assert.Equal(submission.Id, result.Id);
    }

    [Fact]
    public async Task GetPendingTagAsync_NoPending_ReturnsNull()
    {
        gamePlayerRepository.GetAsync(GameId, TargetId, Arg.Any<CancellationToken>())
            .Returns(GamePlayer.Create(GameId, TargetId, GameRole.Player));
        tagRepository.GetPendingByTargetIdAsync(TargetId, Arg.Any<CancellationToken>())
            .Returns(new List<TagSubmission>());

        var result = await sut.GetPendingTagAsync(TargetId, GameId);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingTagAsync_NotMember_ThrowsUnauthorizedException()
    {
        gamePlayerRepository.GetAsync(GameId, TargetId, Arg.Any<CancellationToken>())
            .Returns((GamePlayer?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.GetPendingTagAsync(TargetId, GameId));
    }

    [Fact]
    public async Task GetPendingTagAsync_PendingFromDifferentGame_ReturnsNull()
    {
        var otherGameId = Guid.NewGuid();
        var otherAssignment = Assignment.Create(otherGameId, HunterId, TargetId,
            new List<Condition> { AloneCondition.Create() });
        var submission = TagSubmission.Create(otherAssignment.Id, HunterId, TargetId, Guid.NewGuid());

        gamePlayerRepository.GetAsync(GameId, TargetId, Arg.Any<CancellationToken>())
            .Returns(GamePlayer.Create(GameId, TargetId, GameRole.Player));
        tagRepository.GetPendingByTargetIdAsync(TargetId, Arg.Any<CancellationToken>())
            .Returns(new List<TagSubmission> { submission });
        assignmentRepository.GetByIdAsync(submission.AssignmentId, Arg.Any<CancellationToken>())
            .Returns(otherAssignment);

        var result = await sut.GetPendingTagAsync(TargetId, GameId);
        Assert.Null(result);
    }
}
