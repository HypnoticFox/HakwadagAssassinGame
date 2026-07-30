using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;
using NSubstitute;

namespace HakwadagAssassinGame.Tests.Unit.Application;

public sealed class LeaderboardServiceTests
{
    private readonly IGameRepository gameRepository = Substitute.For<IGameRepository>();
    private readonly IGamePlayerRepository gamePlayerRepository = Substitute.For<IGamePlayerRepository>();
    private readonly IPlayerRepository playerRepository = Substitute.For<IPlayerRepository>();
    private readonly IAssignmentRepository assignmentRepository = Substitute.For<IAssignmentRepository>();
    private readonly LeaderboardService sut;

    private static readonly Guid GameId = Guid.NewGuid();
    private static readonly Guid Player1Id = Guid.NewGuid();
    private static readonly Guid Player2Id = Guid.NewGuid();
    private static readonly Guid Player3Id = Guid.NewGuid();

    public LeaderboardServiceTests()
    {
        sut = new LeaderboardService(
            gameRepository, gamePlayerRepository, playerRepository, assignmentRepository);
    }

    // ── GetLeaderboardAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetLeaderboardAsync_Valid_ReturnsOrderedEntries()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        var player1 = Player.Create("alice@test.com", "Alice", id: Player1Id);
        var player2 = Player.Create("bob@test.com", "Bob", id: Player2Id);
        var player3 = Player.Create("charlie@test.com", "Charlie", id: Player3Id);

        playerRepository.GetByIdAsync(Player1Id, Arg.Any<CancellationToken>()).Returns(player1);
        playerRepository.GetByIdAsync(Player2Id, Arg.Any<CancellationToken>()).Returns(player2);
        playerRepository.GetByIdAsync(Player3Id, Arg.Any<CancellationToken>()).Returns(player3);

        var memberships = new List<GamePlayer>
        {
            GamePlayer.Create(GameId, Player1Id, GameRole.Player),
            GamePlayer.Create(GameId, Player2Id, GameRole.Player),
            GamePlayer.Create(GameId, Player3Id, GameRole.Player),
        };
        // Set scores
        memberships[0].AddScore(50);  // Alice: 50
        memberships[1].AddScore(100); // Bob: 100 (highest score)
        memberships[2].AddScore(50);  // Charlie: 50 (same as Alice)

        gamePlayerRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(memberships);

        // Assignments: Bob has 3 completions, Alice has 2, Charlie has 1
        var bobAssignments = new List<Assignment>
        {
            Assignment.Create(GameId, Player2Id, Player1Id,
                new List<Condition> { AloneCondition.Create() }),
            Assignment.Create(GameId, Player2Id, Player3Id,
                new List<Condition> { AloneCondition.Create() }),
            Assignment.Create(GameId, Player2Id, Player1Id,
                new List<Condition> { AloneCondition.Create() }),
        };
        bobAssignments[0].Complete();
        bobAssignments[1].Complete();
        bobAssignments[2].Complete();

        var aliceAssignments = new List<Assignment>
        {
            Assignment.Create(GameId, Player1Id, Player2Id,
                new List<Condition> { AloneCondition.Create() }),
            Assignment.Create(GameId, Player1Id, Player3Id,
                new List<Condition> { AloneCondition.Create() }),
        };
        aliceAssignments[0].Complete();
        aliceAssignments[1].Complete();

        var charlieAssignments = new List<Assignment>
        {
            Assignment.Create(GameId, Player3Id, Player1Id,
                new List<Condition> { AloneCondition.Create() }),
        };
        charlieAssignments[0].Complete();

        var allAssignments = new List<Assignment>();
        allAssignments.AddRange(bobAssignments);
        allAssignments.AddRange(aliceAssignments);
        allAssignments.AddRange(charlieAssignments);

        assignmentRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(allAssignments);

        var result = await sut.GetLeaderboardAsync(GameId);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        // Bob first: 100 score, 3 tags
        Assert.Equal(Player2Id, result[0].Player.Id);
        Assert.Equal(100, result[0].Score);
        Assert.Equal(3, result[0].Tags);

        // Alice second: 50 score, 2 tags
        Assert.Equal(Player1Id, result[1].Player.Id);
        Assert.Equal(50, result[1].Score);
        Assert.Equal(2, result[1].Tags);

        // Charlie third: 50 score, 1 tag
        Assert.Equal(Player3Id, result[2].Player.Id);
        Assert.Equal(50, result[2].Score);
        Assert.Equal(1, result[2].Tags);
    }

    [Fact]
    public async Task GetLeaderboardAsync_SameScoreSameTags_OrdersByDisplayName()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        var playerA = Player.Create("a@test.com", "Alice", id: Player1Id);
        var playerB = Player.Create("b@test.com", "bob", id: Player2Id); // lowercase b
        var playerC = Player.Create("c@test.com", "Charlie", id: Player3Id);

        playerRepository.GetByIdAsync(Player1Id, Arg.Any<CancellationToken>()).Returns(playerA);
        playerRepository.GetByIdAsync(Player2Id, Arg.Any<CancellationToken>()).Returns(playerB);
        playerRepository.GetByIdAsync(Player3Id, Arg.Any<CancellationToken>()).Returns(playerC);

        var memberships = new List<GamePlayer>
        {
            GamePlayer.Create(GameId, Player1Id, GameRole.Player),
            GamePlayer.Create(GameId, Player2Id, GameRole.Player),
            GamePlayer.Create(GameId, Player3Id, GameRole.Player),
        };
        // All same score and tags
        memberships[0].AddScore(50);
        memberships[1].AddScore(50);
        memberships[2].AddScore(50);

        gamePlayerRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(memberships);
        assignmentRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>())
            .Returns(new List<Assignment>());

        var result = await sut.GetLeaderboardAsync(GameId);

        Assert.Equal(3, result.Count);
        // Alice (ordinal ignore-case), bob, Charlie
        Assert.Equal(Player1Id, result[0].Player.Id); // Alice
        Assert.Equal(Player2Id, result[1].Player.Id); // bob
        Assert.Equal(Player3Id, result[2].Player.Id); // Charlie
    }

    [Fact]
    public async Task GetLeaderboardAsync_GameNotFound_ThrowsGameNotFoundException()
    {
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns((Game?)null);

        await Assert.ThrowsAsync<GameNotFoundException>(() =>
            sut.GetLeaderboardAsync(GameId));
    }

    [Fact]
    public async Task GetLeaderboardAsync_EmptyGame_ReturnsEmptyList()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer>());
        assignmentRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>())
            .Returns(new List<Assignment>());

        var result = await sut.GetLeaderboardAsync(GameId);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLeaderboardAsync_OnlyCompletedTags_AreCounted()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        var player = Player.Create("player@test.com", "Player", id: Player1Id);
        playerRepository.GetByIdAsync(Player1Id, Arg.Any<CancellationToken>()).Returns(player);

        var membership = GamePlayer.Create(GameId, Player1Id, GameRole.Player);
        membership.AddScore(100);
        gamePlayerRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { membership });

        // Mix of active, completed, voided assignments
        var assignments = new List<Assignment>
        {
            Assignment.Create(GameId, Player1Id, Guid.NewGuid(),
                new List<Condition> { AloneCondition.Create() }), // Active
            Assignment.Create(GameId, Player1Id, Guid.NewGuid(),
                new List<Condition> { AloneCondition.Create() }), // Will be completed
            Assignment.Create(GameId, Player1Id, Guid.NewGuid(),
                new List<Condition> { AloneCondition.Create() }), // Voided
        };
        assignments[1].Complete(); // Completed
        assignments[2].Void();     // Voided

        assignmentRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(assignments);

        var result = await sut.GetLeaderboardAsync(GameId);

        Assert.Single(result);
        // Only completed tags are counted (1 out of 3)
        Assert.Equal(Player1Id, result[0].Player.Id);
        Assert.Equal(100, result[0].Score);
        Assert.Equal(1, result[0].Tags);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ExcludesNonParticipatingPlayers()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        var player1 = Player.Create("active@test.com", "ActivePlayer", id: Player1Id);
        var player2 = Player.Create("left@test.com", "LeftPlayer", id: Player2Id);

        playerRepository.GetByIdAsync(Player1Id, Arg.Any<CancellationToken>()).Returns(player1);
        playerRepository.GetByIdAsync(Player2Id, Arg.Any<CancellationToken>()).Returns(player2);

        var activeMembership = GamePlayer.Create(GameId, Player1Id, GameRole.Player);
        activeMembership.AddScore(50);

        var leftMembership = GamePlayer.Create(GameId, Player2Id, GameRole.Player);
        leftMembership.SetParticipating(false); // Left but still active member
        leftMembership.AddScore(100); // Has score but should not appear

        gamePlayerRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { activeMembership, leftMembership });
        assignmentRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>())
            .Returns(new List<Assignment>());

        var result = await sut.GetLeaderboardAsync(GameId);

        // Only the active participating player should be in the leaderboard
        Assert.Single(result);
        Assert.Equal(Player1Id, result[0].Player.Id);
        Assert.Equal(50, result[0].Score);

        // Left player with higher score should not appear
        Assert.DoesNotContain(result, e => e.Player.Id == Player2Id);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ExcludesInactiveMembers()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns(game);

        var player1 = Player.Create("active@test.com", "ActivePlayer", id: Player1Id);
        var player2 = Player.Create("deactivated@test.com", "DeactivatedPlayer", id: Player2Id);

        playerRepository.GetByIdAsync(Player1Id, Arg.Any<CancellationToken>()).Returns(player1);
        playerRepository.GetByIdAsync(Player2Id, Arg.Any<CancellationToken>()).Returns(player2);

        var activeMembership = GamePlayer.Create(GameId, Player1Id, GameRole.Player);
        activeMembership.AddScore(50);

        var deactivatedMembership = GamePlayer.Create(GameId, Player2Id, GameRole.Player);
        deactivatedMembership.Deactivate(); // Permanently left
        deactivatedMembership.AddScore(75);

        gamePlayerRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { activeMembership, deactivatedMembership });
        assignmentRepository.GetByGameIdAsync(GameId, Arg.Any<CancellationToken>())
            .Returns(new List<Assignment>());

        var result = await sut.GetLeaderboardAsync(GameId);

        Assert.Single(result);
        Assert.Equal(Player1Id, result[0].Player.Id);
    }
}
