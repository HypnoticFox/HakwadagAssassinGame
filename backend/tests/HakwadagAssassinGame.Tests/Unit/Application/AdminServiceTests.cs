using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;
using NSubstitute;

namespace HakwadagAssassinGame.Tests.Unit.Application;

public sealed class AdminServiceTests
{
    private readonly IGameRepository gameRepository = Substitute.For<IGameRepository>();
    private readonly IGamePlayerRepository gamePlayerRepository = Substitute.For<IGamePlayerRepository>();
    private readonly IConditionLibrary conditionLibrary = Substitute.For<IConditionLibrary>();
    private readonly AdminService sut;

    private static readonly Guid CreatorId = Guid.NewGuid();
    private static readonly Guid CoAdminId = Guid.NewGuid();
    private static readonly Guid PlayerId = Guid.NewGuid();
    private static readonly Guid GameId = Guid.NewGuid();

    public AdminServiceTests()
    {
        sut = new AdminService(gameRepository, gamePlayerRepository, conditionLibrary);
    }

    // ── AddCoAdminAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task AddCoAdminAsync_Valid_PromotesToCoAdmin()
    {
        var creatorMembership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);
        var targetMembership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(creatorMembership);
        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(targetMembership);

        await sut.AddCoAdminAsync(CreatorId, GameId, PlayerId);

        Assert.Equal(GameRole.CoAdmin, targetMembership.Role);
        await gamePlayerRepository.Received(1).UpdateAsync(targetMembership, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddCoAdminAsync_NotCreator_ThrowsUnauthorizedException()
    {
        var playerMembership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);

        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(playerMembership);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.AddCoAdminAsync(PlayerId, GameId, CoAdminId));
    }

    [Fact]
    public async Task AddCoAdminAsync_InactiveTarget_ThrowsInvalidGameStateException()
    {
        var creatorMembership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);
        var targetMembership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        targetMembership.Deactivate();

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(creatorMembership);
        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(targetMembership);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.AddCoAdminAsync(CreatorId, GameId, PlayerId));
    }

    [Fact]
    public async Task AddCoAdminAsync_TargetNotMember_ThrowsUnauthorizedException()
    {
        var creatorMembership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(creatorMembership);
        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns((GamePlayer?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.AddCoAdminAsync(CreatorId, GameId, PlayerId));
    }

    // ── RemoveCoAdminAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task RemoveCoAdminAsync_Valid_DemotesToPlayer()
    {
        var creatorMembership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);
        var coAdminMembership = GamePlayer.Create(GameId, CoAdminId, GameRole.CoAdmin);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(creatorMembership);
        gamePlayerRepository.GetAsync(GameId, CoAdminId, Arg.Any<CancellationToken>()).Returns(coAdminMembership);

        await sut.RemoveCoAdminAsync(CreatorId, GameId, CoAdminId);

        Assert.Equal(GameRole.Player, coAdminMembership.Role);
        await gamePlayerRepository.Received(1).UpdateAsync(coAdminMembership, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveCoAdminAsync_NotCreator_ThrowsUnauthorizedException()
    {
        var playerMembership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);

        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(playerMembership);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.RemoveCoAdminAsync(PlayerId, GameId, CoAdminId));
    }

    [Fact]
    public async Task RemoveCoAdminAsync_TargetNotMember_ThrowsUnauthorizedException()
    {
        var creatorMembership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(creatorMembership);
        gamePlayerRepository.GetAsync(GameId, CoAdminId, Arg.Any<CancellationToken>()).Returns((GamePlayer?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.RemoveCoAdminAsync(CreatorId, GameId, CoAdminId));
    }

    // ── AddSafeTimeBlockAsync ──────────────────────────────────────────────

    [Fact]
    public async Task AddSafeTimeBlockAsync_Valid_AddsBlock()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        var request = new AddSafeTimeBlockRequest(
            new DateTimeOffset(2025, 6, 15, 22, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2025, 6, 15, 6, 0, 0, TimeSpan.FromHours(2)));
        var blockId = await sut.AddSafeTimeBlockAsync(CreatorId, GameId, request);

        Assert.NotEqual(Guid.Empty, blockId);
        Assert.Single(game.SafeTimeBlocks);
        await gameRepository.Received(1).UpdateAsync(game, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddSafeTimeBlockAsync_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.AddSafeTimeBlockAsync(CreatorId, GameId, null!));
    }

    [Fact]
    public async Task AddSafeTimeBlockAsync_NotAdmin_ThrowsUnauthorizedException()
    {
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.AddSafeTimeBlockAsync(PlayerId, GameId,
                new AddSafeTimeBlockRequest(
                    new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2025, 6, 15, 1, 0, 0, TimeSpan.Zero))));
    }

    [Fact]
    public async Task AddSafeTimeBlockAsync_GameNotFound_ThrowsGameNotFoundException()
    {
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);
        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns((Game?)null);

        await Assert.ThrowsAsync<GameNotFoundException>(() =>
            sut.AddSafeTimeBlockAsync(CreatorId, GameId,
                new AddSafeTimeBlockRequest(
                    new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2025, 6, 15, 1, 0, 0, TimeSpan.Zero))));
    }

    // ── RemoveSafeTimeBlockAsync ───────────────────────────────────────────

    [Fact]
    public async Task RemoveSafeTimeBlockAsync_Valid_RemovesBlock()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            safeTimeBlocks: new List<SafeTimeBlock>
            {
                SafeTimeBlock.Create(
                    new DateTimeOffset(2025, 6, 15, 22, 0, 0, TimeSpan.FromHours(2)),
                    new DateTimeOffset(2025, 6, 15, 6, 0, 0, TimeSpan.FromHours(2)),
                    id: Guid.NewGuid())
            },
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var blockId = game.SafeTimeBlocks[0].Id;
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await sut.RemoveSafeTimeBlockAsync(CreatorId, GameId, blockId);

        Assert.Empty(game.SafeTimeBlocks);
        await gameRepository.Received(1).UpdateAsync(game, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveSafeTimeBlockAsync_BlockNotFound_ThrowsInvalidGameStateException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.RemoveSafeTimeBlockAsync(CreatorId, GameId, Guid.NewGuid()));
    }

    [Fact]
    public async Task RemoveSafeTimeBlockAsync_NotAdmin_ThrowsUnauthorizedException()
    {
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.RemoveSafeTimeBlockAsync(PlayerId, GameId, Guid.NewGuid()));
    }

    // ── AddCustomConditionAsync ────────────────────────────────────────────

    [Fact]
    public async Task AddCustomConditionAsync_Valid_AddsCondition()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        var request = new AddCustomConditionRequest("Must be holding a coffee cup");
        await sut.AddCustomConditionAsync(CreatorId, GameId, request);

        await conditionLibrary.Received(1).AddAsync(
            GameId,
            Arg.Is<CustomCondition>(c => c.Description == "Must be holding a coffee cup"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddCustomConditionAsync_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.AddCustomConditionAsync(CreatorId, GameId, null!));
    }

    [Fact]
    public async Task AddCustomConditionAsync_EmptyDescription_ThrowsArgumentException()
    {
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);
        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>())
            .Returns(Game.Create("Test", "CODE", DateTimeOffset.UtcNow.AddDays(1), 10, 100));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.AddCustomConditionAsync(CreatorId, GameId, new AddCustomConditionRequest("")));
    }

    [Fact]
    public async Task AddCustomConditionAsync_WhitespaceDescription_ThrowsArgumentException()
    {
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);
        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>())
            .Returns(Game.Create("Test", "CODE", DateTimeOffset.UtcNow.AddDays(1), 10, 100));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.AddCustomConditionAsync(CreatorId, GameId, new AddCustomConditionRequest("   ")));
    }

    [Fact]
    public async Task AddCustomConditionAsync_NotAdmin_ThrowsUnauthorizedException()
    {
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.AddCustomConditionAsync(PlayerId, GameId, new AddCustomConditionRequest("Some condition")));
    }

    [Fact]
    public async Task AddCustomConditionAsync_GameNotFound_ThrowsGameNotFoundException()
    {
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);
        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns((Game?)null);

        await Assert.ThrowsAsync<GameNotFoundException>(() =>
            sut.AddCustomConditionAsync(CreatorId, GameId, new AddCustomConditionRequest("Some condition")));
    }

    // ── SetParticipationAsync ────────────────────────────────────────────

    [Fact]
    public async Task SetParticipationAsync_AdminCanToggle()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        // Default is participating
        Assert.True(membership.IsParticipating);

        await sut.SetParticipationAsync(CreatorId, GameId, false);

        Assert.False(membership.IsParticipating);
        await gamePlayerRepository.Received(1).UpdateAsync(membership, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetParticipationAsync_AdminCanToggleBack()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);
        membership.SetParticipating(false);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await sut.SetParticipationAsync(CreatorId, GameId, true);

        Assert.True(membership.IsParticipating);
        await gamePlayerRepository.Received(1).UpdateAsync(membership, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetParticipationAsync_NonAdmin_ThrowsUnauthorizedException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);

        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.SetParticipationAsync(PlayerId, GameId, false));
    }

    [Fact]
    public async Task SetParticipationAsync_AfterGameStarted_ThrowsInvalidGameStateException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        game.Start();
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.SetParticipationAsync(CreatorId, GameId, false));
    }

    // ── UpdateDurationAsync ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateDurationAsync_CreatorWhenNotStarted_UpdatesScheduledEnd()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        var before = DateTimeOffset.UtcNow;
        await sut.UpdateDurationAsync(CreatorId, GameId, new UpdateDurationRequest(48));
        var after = DateTimeOffset.UtcNow;

        Assert.NotNull(game.ScheduledEndAt);
        Assert.InRange(game.ScheduledEndAt!.Value, before.AddHours(48), after.AddHours(48));
        await gameRepository.Received(1).UpdateAsync(game, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDurationAsync_NotCreator_ThrowsUnauthorizedException()
    {
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.UpdateDurationAsync(PlayerId, GameId, new UpdateDurationRequest(48)));
    }

    [Fact]
    public async Task UpdateDurationAsync_GameActive_ThrowsInvalidGameStateException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        game.Start();
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.UpdateDurationAsync(CreatorId, GameId, new UpdateDurationRequest(48)));
    }

    [Fact]
    public async Task UpdateDurationAsync_NonPositiveDuration_ThrowsInvalidGameStateException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.UpdateDurationAsync(CreatorId, GameId, new UpdateDurationRequest(0)));
    }

    // ── ExtendDurationAsync ───────────────────────────────────────────────

    [Fact]
    public async Task ExtendDurationAsync_CreatorWhenActive_ExtendsScheduledEnd()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        game.Start();
        var original = game.ScheduledEndAt!.Value;
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await sut.ExtendDurationAsync(CreatorId, GameId, new ExtendDurationRequest(60));

        Assert.Equal(original.AddHours(1), game.ScheduledEndAt);
        await gameRepository.Received(1).UpdateAsync(game, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExtendDurationAsync_NotCreator_ThrowsUnauthorizedException()
    {
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.ExtendDurationAsync(PlayerId, GameId, new ExtendDurationRequest(60)));
    }

    [Fact]
    public async Task ExtendDurationAsync_GameNotStarted_ThrowsInvalidGameStateException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.ExtendDurationAsync(CreatorId, GameId, new ExtendDurationRequest(60)));
    }

    [Fact]
    public async Task ExtendDurationAsync_NonPositiveMinutes_ThrowsInvalidGameStateException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        game.Start();
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.ExtendDurationAsync(CreatorId, GameId, new ExtendDurationRequest(0)));
    }

    // ── UpdateConfirmationTimeoutAsync ────────────────────────────────────

    [Fact]
    public async Task UpdateConfirmationTimeoutAsync_AdminWhenActive_UpdatesTimeout()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        game.Start();
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await sut.UpdateConfirmationTimeoutAsync(CreatorId, GameId, new UpdateConfirmationTimeoutRequest(30));

        Assert.Equal(TimeSpan.FromMinutes(30), game.ConfirmationTimeout);
        await gameRepository.Received(1).UpdateAsync(game, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateConfirmationTimeoutAsync_NotAdmin_ThrowsUnauthorizedException()
    {
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.UpdateConfirmationTimeoutAsync(PlayerId, GameId, new UpdateConfirmationTimeoutRequest(30)));
    }

    [Fact]
    public async Task UpdateConfirmationTimeoutAsync_GameNotActive_ThrowsInvalidGameStateException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.UpdateConfirmationTimeoutAsync(CreatorId, GameId, new UpdateConfirmationTimeoutRequest(30)));
    }

    // ── UpdateAssignmentCooldownAsync ─────────────────────────────────────

    [Fact]
    public async Task UpdateAssignmentCooldownAsync_AdminWhenActive_UpdatesCooldown()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        game.Start();
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await sut.UpdateAssignmentCooldownAsync(CreatorId, GameId, new UpdateAssignmentCooldownRequest(10));

        Assert.Equal(10, game.AssignmentCooldownMinutes);
        await gameRepository.Received(1).UpdateAsync(game, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAssignmentCooldownAsync_NegativeMinutes_ThrowsArgumentOutOfRangeException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        game.Start();
        var membership = GamePlayer.Create(GameId, CreatorId, GameRole.Creator);

        gamePlayerRepository.GetAsync(GameId, CreatorId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.UpdateAssignmentCooldownAsync(CreatorId, GameId, new UpdateAssignmentCooldownRequest(-5)));
    }
}
