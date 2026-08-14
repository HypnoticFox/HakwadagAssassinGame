using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;
using NSubstitute;

namespace HakwadagAssassinGame.Tests.Unit.Application;

public sealed class AssignmentServiceTests
{
    private readonly IAssignmentRepository assignmentRepository = Substitute.For<IAssignmentRepository>();
    private readonly IPlayerRepository playerRepository = Substitute.For<IPlayerRepository>();
    private readonly IGamePlayerRepository gamePlayerRepository = Substitute.For<IGamePlayerRepository>();
    private readonly IGameRepository gameRepository = Substitute.For<IGameRepository>();
    private readonly AssignmentService sut;

    private static readonly Guid PlayerId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();
    private static readonly Guid GameId = Guid.NewGuid();

    public AssignmentServiceTests()
    {
        sut = new AssignmentService(assignmentRepository, playerRepository, gamePlayerRepository, gameRepository);
    }

    // ── GetMyAssignmentAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetMyAssignmentAsync_Valid_ReturnsAssignmentDto()
    {
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        var target = Player.Create("target@test.com", "Target", id: TargetId);
        var assignment = Assignment.Create(GameId, PlayerId, TargetId,
            new List<Condition> { AloneCondition.Create() });

        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);
        assignmentRepository.GetActiveByHunterIdAsync(GameId, PlayerId, Arg.Any<CancellationToken>())
            .Returns(assignment);
        playerRepository.GetByIdAsync(TargetId, Arg.Any<CancellationToken>()).Returns(target);
        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>())
            .Returns(Player.Create("hunter@test.com", "Hunter", id: PlayerId));
        gamePlayerRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { membership });

        var result = await sut.GetMyAssignmentAsync(PlayerId, GameId);

        Assert.NotNull(result);
        Assert.Equal(assignment.Id, result.Id);
        Assert.Equal(TargetId, result.Target.Id);
        Assert.Equal("Target", result.Target.DisplayName);
    }

    [Fact]
    public async Task GetMyAssignmentAsync_NotMember_ThrowsUnauthorizedException()
    {
        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>())
            .Returns((GamePlayer?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.GetMyAssignmentAsync(PlayerId, GameId));
    }

    [Fact]
    public async Task GetMyAssignmentAsync_NoActiveAssignment_ThrowsAssignmentNotFoundException()
    {
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);

        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);
        assignmentRepository.GetActiveByHunterIdAsync(GameId, PlayerId, Arg.Any<CancellationToken>())
            .Returns((Assignment?)null);

        await Assert.ThrowsAsync<AssignmentNotFoundException>(() =>
            sut.GetMyAssignmentAsync(PlayerId, GameId));
    }

    [Fact]
    public async Task GetMyAssignmentAsync_NonActiveStatus_ThrowsAssignmentNotFoundException()
    {
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        var assignment = Assignment.Create(GameId, PlayerId, TargetId,
            new List<Condition> { AloneCondition.Create() });
        assignment.Complete(); // Now completed, not active

        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);
        assignmentRepository.GetActiveByHunterIdAsync(GameId, PlayerId, Arg.Any<CancellationToken>())
            .Returns(assignment);

        await Assert.ThrowsAsync<AssignmentNotFoundException>(() =>
            sut.GetMyAssignmentAsync(PlayerId, GameId));
    }

    // ── GetNextAvailabilityAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetNextAvailability_ActiveAssignment_ReturnsNull()
    {
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        var game = Game.Create("Test", "CODE", DateTimeOffset.UtcNow.AddDays(1), 4, 10);
        var active = Assignment.Create(GameId, PlayerId, TargetId, [AloneCondition.Create()]);

        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);
        assignmentRepository.GetActiveByHunterIdAsync(GameId, PlayerId, Arg.Any<CancellationToken>())
            .Returns(active);

        var result = await sut.GetNextAvailabilityAsync(PlayerId, GameId);

        Assert.NotNull(result);
        Assert.Null(result.AvailableAt);
    }

    [Fact]
    public async Task GetNextAvailability_InCooldown_ReturnsCooldownExpiry()
    {
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        var game = Game.Create("Test", "CODE", DateTimeOffset.UtcNow.AddDays(1), 4, 10,
            assignmentCooldownMinutes: 30);
        var latest = Assignment.Create(GameId, PlayerId, TargetId, [AloneCondition.Create()],
            assignedAt: DateTimeOffset.UtcNow.AddMinutes(-10));

        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);
        assignmentRepository.GetActiveByHunterIdAsync(GameId, PlayerId, Arg.Any<CancellationToken>())
            .Returns((Assignment?)null);
        assignmentRepository.GetMostRecentByHunterIdAsync(GameId, PlayerId, Arg.Any<CancellationToken>())
            .Returns(latest);

        var result = await sut.GetNextAvailabilityAsync(PlayerId, GameId);

        Assert.NotNull(result);
        Assert.NotNull(result.AvailableAt);
        // Cooldown expires 30 minutes after the last assignment (10 minutes ago) → ~20 minutes from now.
        Assert.True(result.AvailableAt > DateTimeOffset.UtcNow.AddMinutes(19));
        Assert.True(result.AvailableAt <= DateTimeOffset.UtcNow.AddMinutes(21));
    }

    [Fact]
    public async Task GetNextAvailability_CooldownElapsed_ReturnsNow()
    {
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        var game = Game.Create("Test", "CODE", DateTimeOffset.UtcNow.AddDays(1), 4, 10,
            assignmentCooldownMinutes: 30);
        var latest = Assignment.Create(GameId, PlayerId, TargetId, [AloneCondition.Create()],
            assignedAt: DateTimeOffset.UtcNow.AddHours(-1));

        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);
        assignmentRepository.GetActiveByHunterIdAsync(GameId, PlayerId, Arg.Any<CancellationToken>())
            .Returns((Assignment?)null);
        assignmentRepository.GetMostRecentByHunterIdAsync(GameId, PlayerId, Arg.Any<CancellationToken>())
            .Returns(latest);

        var result = await sut.GetNextAvailabilityAsync(PlayerId, GameId);

        Assert.NotNull(result);
        Assert.NotNull(result.AvailableAt);
        Assert.True(result.AvailableAt <= DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task GetNextAvailability_NoAssignmentHistory_ReturnsNow()
    {
        var membership = GamePlayer.Create(GameId, PlayerId, GameRole.Player);
        var game = Game.Create("Test", "CODE", DateTimeOffset.UtcNow.AddDays(1), 4, 10,
            assignmentCooldownMinutes: 30);

        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);
        assignmentRepository.GetActiveByHunterIdAsync(GameId, PlayerId, Arg.Any<CancellationToken>())
            .Returns((Assignment?)null);
        assignmentRepository.GetMostRecentByHunterIdAsync(GameId, PlayerId, Arg.Any<CancellationToken>())
            .Returns((Assignment?)null);

        var result = await sut.GetNextAvailabilityAsync(PlayerId, GameId);

        Assert.NotNull(result);
        Assert.NotNull(result.AvailableAt);
        Assert.True(result.AvailableAt <= DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task GetNextAvailability_NotMember_ThrowsUnauthorizedException()
    {
        gamePlayerRepository.GetAsync(GameId, PlayerId, Arg.Any<CancellationToken>())
            .Returns((GamePlayer?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.GetNextAvailabilityAsync(PlayerId, GameId));
    }
}
