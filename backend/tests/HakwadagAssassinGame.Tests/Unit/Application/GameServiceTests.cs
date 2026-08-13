using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;
using NSubstitute;

namespace HakwadagAssassinGame.Tests.Unit.Application;

public sealed class GameServiceTests
{
    private readonly IGameRepository gameRepository = Substitute.For<IGameRepository>();
    private readonly IPlayerRepository playerRepository = Substitute.For<IPlayerRepository>();
    private readonly IGamePlayerRepository gamePlayerRepository = Substitute.For<IGamePlayerRepository>();
    private readonly IAssignmentRepository assignmentRepository = Substitute.For<IAssignmentRepository>();
    private readonly ITagSubmissionRepository tagSubmissionRepository = Substitute.For<ITagSubmissionRepository>();
    private readonly IInviteCodeGenerator inviteCodeGenerator = Substitute.For<IInviteCodeGenerator>();
    private readonly IConditionLibrary conditionLibrary = Substitute.For<IConditionLibrary>();
    private readonly GameService sut;

    private static readonly Guid PlayerId = Guid.NewGuid();
    private static readonly Guid OtherPlayerId = Guid.NewGuid();
    private static readonly Guid ThirdPlayerId = Guid.NewGuid();
    private static readonly Guid CoAdminId = Guid.NewGuid();
    private static readonly Guid GameId = Guid.NewGuid();

    public GameServiceTests()
    {
        sut = new GameService(
            gameRepository, playerRepository, gamePlayerRepository,
            assignmentRepository, tagSubmissionRepository,
            inviteCodeGenerator, conditionLibrary);

        inviteCodeGenerator.GenerateCode().Returns("INVITE123");
        conditionLibrary.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Condition>());
    }

    // ── CreateGameAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateGameAsync_ValidRequest_CreatesGameAndReturnsDto()
    {
        var player = Player.Create("creator@test.com", "Creator", id: PlayerId);
        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);

        var request = new CreateGameRequest(
            "Test Game", 24, 10, 100, 15, null, null);
        var createdGame = default(Game);
        gameRepository.When(x => x.AddAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>()))
            .Do(call => createdGame = call.Arg<Game>());
        gameRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var id = call.Arg<Guid>();
                // Create a game with matching properties for the ToDtoAsync call
                var game = Game.Create(
                    "Test Game", "INVITE123",
                    DateTimeOffset.UtcNow.AddHours(24), 10, 100,
                    confirmationTimeout: TimeSpan.FromMinutes(15));
                // Override the id to match what was passed
                typeof(Game).GetProperty(nameof(Game.Id))!.SetValue(game, id);
                return game;
            });

        gamePlayerRepository.GetByGameIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer>
            {
                GamePlayer.Create(GameId, PlayerId, GameRole.Creator)
            });

        var result = await sut.CreateGameAsync(PlayerId, request);

        Assert.NotNull(result);
        await gameRepository.Received(1).AddAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>());
        await gamePlayerRepository.Received(1).AddAsync(
            Arg.Is<GamePlayer>(gp => gp.PlayerId == PlayerId && gp.Role == GameRole.Creator),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateGameAsync_PlayerNotFound_ThrowsPlayerNotFoundException()
    {
        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns((Player?)null);
        var request = new CreateGameRequest("Test", 24, 10, 100, 15, null, null);

        await Assert.ThrowsAsync<PlayerNotFoundException>(() => sut.CreateGameAsync(PlayerId, request));
    }

    [Fact]
    public async Task CreateGameAsync_NullRequest_ThrowsArgumentNullException()
    {
        var player = Player.Create("creator@test.com", "Creator", id: PlayerId);
        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreateGameAsync(PlayerId, null!));
    }

    [Fact]
    public async Task CreateGameAsync_ZeroDuration_ThrowsInvalidGameStateException()
    {
        var player = Player.Create("creator@test.com", "Creator", id: PlayerId);
        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        var request = new CreateGameRequest("Test", 0, 10, 100, 15, null, null);

        await Assert.ThrowsAsync<InvalidGameStateException>(() => sut.CreateGameAsync(PlayerId, request));
    }

    [Fact]
    public async Task CreateGameAsync_NegativeDuration_ThrowsInvalidGameStateException()
    {
        var player = Player.Create("creator@test.com", "Creator", id: PlayerId);
        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        var request = new CreateGameRequest("Test", -1, 10, 100, 15, null, null);

        await Assert.ThrowsAsync<InvalidGameStateException>(() => sut.CreateGameAsync(PlayerId, request));
    }

    [Fact]
    public async Task CreateGameAsync_ZeroConfirmationTimeout_ThrowsInvalidGameStateException()
    {
        var player = Player.Create("creator@test.com", "Creator", id: PlayerId);
        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        var request = new CreateGameRequest("Test", 24, 10, 100, 0, null, null);

        await Assert.ThrowsAsync<InvalidGameStateException>(() => sut.CreateGameAsync(PlayerId, request));
    }

    [Fact]
    public async Task CreateGameAsync_NullDuration_CreatesGameWithoutScheduledEnd()
    {
        var player = Player.Create("creator@test.com", "Creator", id: PlayerId);
        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);

        var request = new CreateGameRequest(
            "Test Game", null, 10, 100, 15, null, null);
        var createdGame = default(Game);
        gameRepository.When(x => x.AddAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>()))
            .Do(call => createdGame = call.Arg<Game>());
        gamePlayerRepository.GetByGameIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer>
            {
                GamePlayer.Create(GameId, PlayerId, GameRole.Creator)
            });

        var result = await sut.CreateGameAsync(PlayerId, request);

        Assert.NotNull(createdGame);
        Assert.Null(createdGame!.ScheduledEndAt);
        Assert.NotNull(result);
        await gameRepository.Received(1).AddAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>());
    }

    // ── JoinGameAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task JoinGameAsync_ValidInvite_AddsPlayerAndReturnsDto()
    {
        var player = Player.Create("player@test.com", "Player", id: PlayerId);
        var game = Game.Create("TestGame", "INVITE123",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        gameRepository.GetByInviteCodeAsync("INVITE123", Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>())
            .Returns((GamePlayer?)null);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer>
            {
                GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Creator)
            });

        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>())
            .Returns((GamePlayer?)null);

        // For the ToDtoAsync call after join
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer>
            {
                GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Creator),
                GamePlayer.Create(game.Id, PlayerId, GameRole.Player)
            });

        var result = await sut.JoinGameAsync(PlayerId, "  INVITE123  ", "  PlayerName  ");

        Assert.NotNull(result);
        Assert.Equal(game.Id, result.Id);
        await playerRepository.Received(1).UpdateAsync(Arg.Is<Player>(p => p.DisplayName == "PlayerName"),
            Arg.Any<CancellationToken>());
        await gamePlayerRepository.Received(1).AddAsync(
            Arg.Is<GamePlayer>(gp => gp.PlayerId == PlayerId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinGameAsync_WithRequestObject_DelegatesToCoreMethod()
    {
        var player = Player.Create("player@test.com", "Player", id: PlayerId);
        var game = Game.Create("TestGame", "INVITE123",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        gameRepository.GetByInviteCodeAsync("INVITE123", Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>())
            .Returns((GamePlayer?)null);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer>
            {
                GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Creator),
                GamePlayer.Create(game.Id, PlayerId, GameRole.Player)
            });

        var result = await sut.JoinGameAsync(PlayerId, "INVITE123",
            new JoinGameRequest("PlayerName"));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task JoinGameAsync_PlayerNotFound_ThrowsPlayerNotFoundException()
    {
        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns((Player?)null);

        await Assert.ThrowsAsync<PlayerNotFoundException>(() =>
            sut.JoinGameAsync(PlayerId, "CODE", "Player"));
    }

    [Fact]
    public async Task JoinGameAsync_EmptyInviteCode_ThrowsArgumentException()
    {
        var player = Player.Create("player@test.com", "Player", id: PlayerId);
        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.JoinGameAsync(PlayerId, "", "Player"));
    }

    [Fact]
    public async Task JoinGameAsync_EmptyDisplayName_ThrowsArgumentException()
    {
        var player = Player.Create("player@test.com", "Player", id: PlayerId);
        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.JoinGameAsync(PlayerId, "CODE", ""));
    }

    [Fact]
    public async Task JoinGameAsync_GameNotFound_ThrowsGameNotFoundException()
    {
        var player = Player.Create("player@test.com", "Player", id: PlayerId);
        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        gameRepository.GetByInviteCodeAsync("CODE", Arg.Any<CancellationToken>()).Returns((Game?)null);

        await Assert.ThrowsAsync<GameNotFoundException>(() =>
            sut.JoinGameAsync(PlayerId, "CODE", "Player"));
    }

    [Fact]
    public async Task JoinGameAsync_GameEnded_ThrowsInvalidGameStateException()
    {
        var player = Player.Create("player@test.com", "Player", id: PlayerId);
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100);
        game.Start();
        game.End();

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        gameRepository.GetByInviteCodeAsync("CODE", Arg.Any<CancellationToken>()).Returns(game);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.JoinGameAsync(PlayerId, "CODE", "Player"));
    }

    [Fact]
    public async Task JoinGameAsync_AlreadyActiveMember_ThrowsInvalidGameStateException()
    {
        var player = Player.Create("player@test.com", "Player", id: PlayerId);
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100);

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        gameRepository.GetByInviteCodeAsync("CODE", Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>())
            .Returns(GamePlayer.Create(game.Id, PlayerId, GameRole.Player));

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.JoinGameAsync(PlayerId, "CODE", "Player"));
    }

    [Fact]
    public async Task JoinGameAsync_GameFull_ThrowsInvalidGameStateException()
    {
        var player = Player.Create("player@test.com", "Player", id: PlayerId);
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 3, 100);
        var existingMembers = new List<GamePlayer>
        {
            GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Creator),
            GamePlayer.Create(game.Id, ThirdPlayerId, GameRole.Player),
            GamePlayer.Create(game.Id, CoAdminId, GameRole.Player),
        };

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        gameRepository.GetByInviteCodeAsync("CODE", Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>())
            .Returns((GamePlayer?)null);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(existingMembers);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.JoinGameAsync(PlayerId, "CODE", "Player"));
    }

    [Fact]
    public async Task JoinGameAsync_DuringActive_NewPlayer_AddsMembership()
    {
        var player = Player.Create("player@test.com", "Player", id: PlayerId);
        var game = Game.Create("TestGame", "INVITE123",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        game.Start();

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        gameRepository.GetByInviteCodeAsync("INVITE123", Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>())
            .Returns((GamePlayer?)null);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer>
            {
                GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Creator),
                GamePlayer.Create(game.Id, ThirdPlayerId, GameRole.Player),
            });

        // After join, ToDtoAsync sees the new member
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer>
            {
                GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Creator),
                GamePlayer.Create(game.Id, ThirdPlayerId, GameRole.Player),
                GamePlayer.Create(game.Id, PlayerId, GameRole.Player),
            });

        var result = await sut.JoinGameAsync(PlayerId, "INVITE123", "PlayerName");

        Assert.NotNull(result);
        await gamePlayerRepository.Received(1).AddAsync(
            Arg.Is<GamePlayer>(gp => gp.PlayerId == PlayerId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinGameAsync_PermanentlyLeft_ThrowsInvalidGameStateException()
    {
        var player = Player.Create("player@test.com", "Player", id: PlayerId);
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100);
        var membership = GamePlayer.Create(game.Id, PlayerId, GameRole.Player);
        membership.Deactivate(); // IsActive = false (permanently left)

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        gameRepository.GetByInviteCodeAsync("CODE", Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>())
            .Returns(membership);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.JoinGameAsync(PlayerId, "CODE", "Player"));
    }

    [Fact]
    public async Task JoinGameAsync_RejoinDuringActive_ResetsScoreAndCreatesAssignment()
    {
        var player = Player.Create("player@test.com", "Player", id: PlayerId);
        var game = Game.Create("TestGame", "INVITE123",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        game.Start();

        // Player who left (IsParticipating=false, IsActive=true, non-zero score)
        var membership = GamePlayer.Create(game.Id, PlayerId, GameRole.Player);
        membership.SetParticipating(false);
        membership.AddScore(50); // Had some score before leaving
        // ResetScore will set it back to 0

        var otherMembership = GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Player);
        var thirdMembership = GamePlayer.Create(game.Id, ThirdPlayerId, GameRole.Player);

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        playerRepository.GetByIdAsync(OtherPlayerId, Arg.Any<CancellationToken>())
            .Returns(Player.Create("other@test.com", "Other", id: OtherPlayerId));
        playerRepository.GetByIdAsync(ThirdPlayerId, Arg.Any<CancellationToken>())
            .Returns(Player.Create("third@test.com", "Third", id: ThirdPlayerId));
        gameRepository.GetByInviteCodeAsync("INVITE123", Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>())
            .Returns(membership);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { membership, otherMembership, thirdMembership });
        assignmentRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Assignment>());
        conditionLibrary.GetAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Condition> { AloneCondition.Create() });

        await sut.JoinGameAsync(PlayerId, "INVITE123", "PlayerName");

        // Score was reset
        Assert.Equal(0, membership.Score);
        // Participation restored
        Assert.True(membership.IsParticipating);
        // IsActive unchanged (still true)
        Assert.True(membership.IsActive);
        // A new assignment was created for the rejoining player
        await assignmentRepository.Received(1).AddAsync(
            Arg.Is<Assignment>(a => a.HunterId == PlayerId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinGameAsync_RejoinDuringNotStarted_DoesNotCreateAssignment()
    {
        var player = Player.Create("player@test.com", "Player", id: PlayerId);
        var game = Game.Create("TestGame", "INVITE123",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        // Game is NotStarted

        var membership = GamePlayer.Create(game.Id, PlayerId, GameRole.Player);
        membership.SetParticipating(false);
        membership.AddScore(30);

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        gameRepository.GetByInviteCodeAsync("INVITE123", Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>())
            .Returns(membership);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { membership, GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Creator) });

        // ToDtoAsync setup
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer>
            {
                membership,
                GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Creator),
            });

        await sut.JoinGameAsync(PlayerId, "INVITE123", "PlayerName");

        // Score was reset
        Assert.Equal(0, membership.Score);
        // Participation restored
        Assert.True(membership.IsParticipating);
        // No assignment created for NotStarted game
        await assignmentRepository.DidNotReceiveWithAnyArgs().AddAsync(Arg.Any<Assignment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinGameAsync_RejoinWhenAlreadyParticipating_Throws()
    {
        var player = Player.Create("player@test.com", "Player", id: PlayerId);
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100);

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(player);
        gameRepository.GetByInviteCodeAsync("CODE", Arg.Any<CancellationToken>()).Returns(game);
        // Membership is active and participating (default)
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>())
            .Returns(GamePlayer.Create(game.Id, PlayerId, GameRole.Player));

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.JoinGameAsync(PlayerId, "CODE", "Player"));
    }

    // ── StartGameAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task StartGameAsync_Valid_StartsGameAndCreatesAssignments()
    {
        var creator = Player.Create("creator@test.com", "Creator", id: PlayerId);
        var otherPlayer = Player.Create("other@test.com", "Other", id: OtherPlayerId);
        var thirdPlayer = Player.Create("third@test.com", "Third", id: ThirdPlayerId);
        var game = Game.Create("TestGame", "INVITE123",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(creator);
        playerRepository.GetByIdAsync(OtherPlayerId, Arg.Any<CancellationToken>()).Returns(otherPlayer);
        playerRepository.GetByIdAsync(ThirdPlayerId, Arg.Any<CancellationToken>()).Returns(thirdPlayer);
        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);

        var creatorMembership = GamePlayer.Create(game.Id, PlayerId, GameRole.Creator);
        var otherMembership = GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Player);
        var thirdMembership = GamePlayer.Create(game.Id, ThirdPlayerId, GameRole.Player);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns(creatorMembership);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { creatorMembership, otherMembership, thirdMembership });
        conditionLibrary.GetAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Condition> { AloneCondition.Create() });

        // For the ToDtoAsync call after start
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { creatorMembership, otherMembership, thirdMembership });

        var result = await sut.StartGameAsync(PlayerId, game.Id);

        Assert.NotNull(result);
        await assignmentRepository.Received(3).AddAsync(Arg.Any<Assignment>(), Arg.Any<CancellationToken>());
        await gameRepository.Received(1).UpdateAsync(Arg.Is<Game>(g => g.Status == GameStatus.Active),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartGameAsync_NotAdmin_ThrowsUnauthorizedException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100);
        var playerMembership = GamePlayer.Create(game.Id, PlayerId, GameRole.Player);

        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns(playerMembership);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.StartGameAsync(PlayerId, game.Id));
    }

    [Fact]
    public async Task StartGameAsync_AnyAdmin_CanStart()
    {
        var coAdmin = Player.Create("coadmin@test.com", "CoAdmin", id: CoAdminId);
        var otherPlayer = Player.Create("other@test.com", "Other", id: OtherPlayerId);
        var thirdPlayer = Player.Create("third@test.com", "Third", id: ThirdPlayerId);
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        playerRepository.GetByIdAsync(CoAdminId, Arg.Any<CancellationToken>()).Returns(coAdmin);
        playerRepository.GetByIdAsync(OtherPlayerId, Arg.Any<CancellationToken>()).Returns(otherPlayer);
        playerRepository.GetByIdAsync(ThirdPlayerId, Arg.Any<CancellationToken>()).Returns(thirdPlayer);
        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);

        var coAdminMembership = GamePlayer.Create(game.Id, CoAdminId, GameRole.CoAdmin);
        var otherMembership = GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Player);
        var thirdMembership = GamePlayer.Create(game.Id, ThirdPlayerId, GameRole.Player);
        gamePlayerRepository.GetAsync(game.Id, CoAdminId, Arg.Any<CancellationToken>()).Returns(coAdminMembership);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { coAdminMembership, otherMembership, thirdMembership });
        conditionLibrary.GetAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Condition> { AloneCondition.Create() });

        // For the ToDtoAsync call after start
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { coAdminMembership, otherMembership, thirdMembership });

        var result = await sut.StartGameAsync(CoAdminId, game.Id);

        Assert.NotNull(result);
        await assignmentRepository.Received(3).AddAsync(Arg.Any<Assignment>(), Arg.Any<CancellationToken>());
        await gameRepository.Received(1).UpdateAsync(Arg.Is<Game>(g => g.Status == GameStatus.Active),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartGameAsync_LessThan3Players_ThrowsInvalidGameStateException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100);
        var creatorMembership = GamePlayer.Create(game.Id, PlayerId, GameRole.Creator);
        var otherMembership = GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Player);

        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns(creatorMembership);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { creatorMembership, otherMembership });

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.StartGameAsync(PlayerId, game.Id));
    }

    [Fact]
    public async Task StartGameAsync_GameNotFound_ThrowsGameNotFoundException()
    {
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns((Game?)null);

        await Assert.ThrowsAsync<GameNotFoundException>(() =>
            sut.StartGameAsync(PlayerId, GameId));
    }

    [Fact]
    public async Task StartGameAsync_NonParticipatingAdmin_CanStart()
    {
        var creator = Player.Create("creator@test.com", "Creator", id: PlayerId);
        var otherPlayer = Player.Create("other@test.com", "Other", id: OtherPlayerId);
        var thirdPlayer = Player.Create("third@test.com", "Third", id: ThirdPlayerId);
        var fourthPlayerId = Guid.NewGuid();
        var fourthPlayer = Player.Create("fourth@test.com", "Fourth", id: fourthPlayerId);
        var game = Game.Create("TestGame", "INVITE123",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(creator);
        playerRepository.GetByIdAsync(OtherPlayerId, Arg.Any<CancellationToken>()).Returns(otherPlayer);
        playerRepository.GetByIdAsync(ThirdPlayerId, Arg.Any<CancellationToken>()).Returns(thirdPlayer);
        playerRepository.GetByIdAsync(fourthPlayerId, Arg.Any<CancellationToken>()).Returns(fourthPlayer);
        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);

        var creatorMembership = GamePlayer.Create(game.Id, PlayerId, GameRole.Creator);
        creatorMembership.SetParticipating(false); // Creator opts out but is still admin
        var otherMembership = GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Player);
        var thirdMembership = GamePlayer.Create(game.Id, ThirdPlayerId, GameRole.Player);
        var fourthMembership = GamePlayer.Create(game.Id, fourthPlayerId, GameRole.Player);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns(creatorMembership);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { creatorMembership, otherMembership, thirdMembership, fourthMembership });
        conditionLibrary.GetAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Condition> { AloneCondition.Create() });

        // For the ToDtoAsync call after start
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { creatorMembership, otherMembership, thirdMembership, fourthMembership });

        var result = await sut.StartGameAsync(PlayerId, game.Id);

        Assert.NotNull(result);
        // 3 participating players → creates 3 assignments
        await assignmentRepository.Received(3).AddAsync(Arg.Any<Assignment>(), Arg.Any<CancellationToken>());
        await gameRepository.Received(1).UpdateAsync(Arg.Is<Game>(g => g.Status == GameStatus.Active),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartGameAsync_NonParticipatingPlayersNotCounted()
    {
        var creator = Player.Create("creator@test.com", "Creator", id: PlayerId);
        var otherPlayer = Player.Create("other@test.com", "Other", id: OtherPlayerId);
        var thirdPlayer = Player.Create("third@test.com", "Third", id: ThirdPlayerId);
        var game = Game.Create("TestGame", "INVITE123",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(creator);
        playerRepository.GetByIdAsync(OtherPlayerId, Arg.Any<CancellationToken>()).Returns(otherPlayer);
        playerRepository.GetByIdAsync(ThirdPlayerId, Arg.Any<CancellationToken>()).Returns(thirdPlayer);
        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);

        var creatorMembership = GamePlayer.Create(game.Id, PlayerId, GameRole.Creator);
        var otherMembership = GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Player);
        otherMembership.SetParticipating(false); // Other opts out
        var thirdMembership = GamePlayer.Create(game.Id, ThirdPlayerId, GameRole.Player);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns(creatorMembership);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { creatorMembership, otherMembership, thirdMembership });

        // Only 2 participating (creator + third) → should throw
        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.StartGameAsync(PlayerId, game.Id));
    }

    // ── EndGameAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task EndGameAsync_Valid_EndsGame()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var creatorMembership = GamePlayer.Create(game.Id, PlayerId, GameRole.Creator);

        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns(creatorMembership);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { creatorMembership });

        var result = await sut.EndGameAsync(PlayerId, game.Id);

        Assert.NotNull(result);
        Assert.Equal(GameStatus.Ended, game.Status);
        await gameRepository.Received(1).UpdateAsync(game, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EndGameAsync_NotAdmin_ThrowsUnauthorizedException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100);
        var playerMembership = GamePlayer.Create(game.Id, PlayerId, GameRole.Player);

        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns(playerMembership);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.EndGameAsync(PlayerId, game.Id));
    }

    [Fact]
    public async Task EndGameAsync_NotMember_ThrowsUnauthorizedException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100);

        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns((GamePlayer?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.EndGameAsync(PlayerId, game.Id));
    }

    // ── GetGameAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetGameAsync_Valid_ReturnsDto()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var membership = GamePlayer.Create(game.Id, PlayerId, GameRole.Player);

        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { membership });

        var result = await sut.GetGameAsync(PlayerId, game.Id);

        Assert.NotNull(result);
        Assert.Equal(game.Id, result.Id);
    }

    [Fact]
    public async Task GetGameAsync_NotMember_ThrowsUnauthorizedException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100);
        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns((GamePlayer?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.GetGameAsync(PlayerId, game.Id));
    }

    [Fact]
    public async Task GetGameAsync_GameNotFound_ThrowsGameNotFoundException()
    {
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns((Game?)null);

        await Assert.ThrowsAsync<GameNotFoundException>(() =>
            sut.GetGameAsync(PlayerId, GameId));
    }

    // ── GetPlayersAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetPlayersAsync_Valid_ReturnsPlayersOrderedByRoleAndName()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        var creator = Player.Create("creator@test.com", "Zeta", id: PlayerId);
        var coAdmin = Player.Create("coadmin@test.com", "Alpha", id: CoAdminId);
        var otherPlayer = Player.Create("other@test.com", "Beta", id: OtherPlayerId);
        var thirdPlayer = Player.Create("third@test.com", "Mike", id: ThirdPlayerId);

        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns(creator);
        playerRepository.GetByIdAsync(CoAdminId, Arg.Any<CancellationToken>()).Returns(coAdmin);
        playerRepository.GetByIdAsync(OtherPlayerId, Arg.Any<CancellationToken>()).Returns(otherPlayer);
        playerRepository.GetByIdAsync(ThirdPlayerId, Arg.Any<CancellationToken>()).Returns(thirdPlayer);
        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer>
            {
                GamePlayer.Create(game.Id, ThirdPlayerId, GameRole.Player),
                GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Player),
                GamePlayer.Create(game.Id, PlayerId, GameRole.Creator),
                GamePlayer.Create(game.Id, CoAdminId, GameRole.CoAdmin),
            });

        var result = await sut.GetPlayersAsync(game.Id);

        Assert.Equal(4, result.Count);
        // Creator first, then CoAdmin, then Players alphabetically by display name
        Assert.Equal(new[] { PlayerId, CoAdminId, OtherPlayerId, ThirdPlayerId },
            result.Select(player => player.PlayerId).ToArray());
        Assert.Equal(GameRole.Creator, result[0].Role);
        Assert.Equal(GameRole.CoAdmin, result[1].Role);
        Assert.Equal(GameRole.Player, result[2].Role);
        Assert.Equal(GameRole.Player, result[3].Role);
        Assert.Equal("Beta", result[2].DisplayName);
        Assert.Equal("Mike", result[3].DisplayName);
        Assert.Equal("creator@test.com", result[0].Email);
    }

    [Fact]
    public async Task GetPlayersAsync_GameNotFound_ThrowsGameNotFoundException()
    {
        gameRepository.GetByIdAsync(GameId, Arg.Any<CancellationToken>()).Returns((Game?)null);

        await Assert.ThrowsAsync<GameNotFoundException>(() => sut.GetPlayersAsync(GameId));
    }

    [Fact]
    public async Task GetPlayersAsync_MissingPlayerRecord_ThrowsPlayerNotFoundException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));

        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer>
            {
                GamePlayer.Create(game.Id, PlayerId, GameRole.Player)
            });
        playerRepository.GetByIdAsync(PlayerId, Arg.Any<CancellationToken>()).Returns((Player?)null);

        await Assert.ThrowsAsync<PlayerNotFoundException>(() => sut.GetPlayersAsync(game.Id));
    }

    // ── GetMyGamesAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyGamesAsync_Valid_ReturnsGames()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var membership = GamePlayer.Create(game.Id, PlayerId, GameRole.Player);

        gamePlayerRepository.GetByPlayerIdAsync(PlayerId, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { membership });
        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { membership });

        var result = await sut.GetMyGamesAsync(PlayerId);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(game.Id, result[0].Id);
    }

    [Fact]
    public async Task GetMyGamesAsync_NoMemberships_ReturnsEmptyList()
    {
        gamePlayerRepository.GetByPlayerIdAsync(PlayerId, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer>());

        var result = await sut.GetMyGamesAsync(PlayerId);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyGamesAsync_SkipsNullGames()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        var membership = GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Player);

        gamePlayerRepository.GetByPlayerIdAsync(PlayerId, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { membership });
        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns((Game?)null);

        var result = await sut.GetMyGamesAsync(PlayerId);
        Assert.Empty(result);
    }

    // ── LeaveGameAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task LeaveGameAsync_Valid_DeactivatesAndReassigns()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        game.Start();
        var membership = GamePlayer.Create(game.Id, PlayerId, GameRole.Player);
        // Manually set IsActive to true via reflection since Create defaults to active
        var otherMembership = GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Player);

        var assignment = Assignment.Create(game.Id, PlayerId, OtherPlayerId,
            new List<Condition> { AloneCondition.Create() });

        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { membership, otherMembership });
        assignmentRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Assignment> { assignment });
        tagSubmissionRepository.GetPendingByTargetIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<TagSubmission>());
        playerRepository.GetByIdAsync(OtherPlayerId, Arg.Any<CancellationToken>())
            .Returns(Player.Create("other@test.com", "Other", id: OtherPlayerId));

        // At least 2 remaining players triggers reassignment
        conditionLibrary.GetAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Condition> { AloneCondition.Create() });

        await sut.LeaveGameAsync(PlayerId, game.Id);

        // During Active game, leaving sets IsParticipating=false, keeps IsActive=true
        Assert.True(membership.IsActive);
        Assert.False(membership.IsParticipating);
        await gamePlayerRepository.Received(1).UpdateAsync(membership, Arg.Any<CancellationToken>());
        await assignmentRepository.Received(1).UpdateAsync(
            Arg.Is<Assignment>(a => a.Status == AssignmentStatus.Voided || a.Status == AssignmentStatus.TargetLeft),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveGameAsync_NotStarted_Deactivates()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100,
            confirmationTimeout: TimeSpan.FromMinutes(15));
        // Game is NotStarted — do not call Start()
        var membership = GamePlayer.Create(game.Id, PlayerId, GameRole.Player);
        var otherMembership = GamePlayer.Create(game.Id, OtherPlayerId, GameRole.Player);

        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);
        gamePlayerRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GamePlayer> { membership, otherMembership });
        assignmentRepository.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Assignment>());
        tagSubmissionRepository.GetPendingByTargetIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<TagSubmission>());

        await sut.LeaveGameAsync(PlayerId, game.Id);

        // During NotStarted game, leaving calls Deactivate()
        Assert.False(membership.IsActive);
        Assert.True(membership.IsParticipating); // unchanged by leave
    }

    [Fact]
    public async Task LeaveGameAsync_AlreadyLeft_ThrowsInvalidGameStateException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100);
        var membership = GamePlayer.Create(game.Id, PlayerId, GameRole.Player);
        membership.Deactivate();

        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns(membership);

        await Assert.ThrowsAsync<InvalidGameStateException>(() =>
            sut.LeaveGameAsync(PlayerId, game.Id));
    }

    [Fact]
    public async Task LeaveGameAsync_NotMember_ThrowsUnauthorizedException()
    {
        var game = Game.Create("TestGame", "CODE",
            DateTimeOffset.UtcNow.AddDays(1), 10, 100);
        gameRepository.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        gamePlayerRepository.GetAsync(game.Id, PlayerId, Arg.Any<CancellationToken>()).Returns((GamePlayer?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.LeaveGameAsync(PlayerId, game.Id));
    }
}
