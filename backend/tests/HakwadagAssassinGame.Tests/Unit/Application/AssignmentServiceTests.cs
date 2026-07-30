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
    private readonly AssignmentService sut;

    private static readonly Guid PlayerId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();
    private static readonly Guid GameId = Guid.NewGuid();

    public AssignmentServiceTests()
    {
        sut = new AssignmentService(assignmentRepository, playerRepository, gamePlayerRepository);
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
}
