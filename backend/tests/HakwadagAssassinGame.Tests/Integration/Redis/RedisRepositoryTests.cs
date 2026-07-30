using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Infrastructure.Persistence.Redis;

namespace HakwadagAssassinGame.Tests.Integration.Redis;

public sealed class RedisRepositoryTests : RedisTestBase
{
    // ── Game Repository ───────────────────────────────────────────────────

    [Fact]
    public async Task GameRepository_AddAndGetById()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisGameRepository(Multiplexer);
        var game = Game.Create("Test Game", "ABC123", DateTimeOffset.UtcNow.AddDays(1), 4, 10);

        await repo.AddAsync(game);
        var retrieved = await repo.GetByIdAsync(game.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(game.Id, retrieved.Id);
        Assert.Equal("Test Game", retrieved.Name);
        Assert.Equal("ABC123", retrieved.InviteCode);
    }

    [Fact]
    public async Task GameRepository_GetById_NonExistent_ReturnsNull()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisGameRepository(Multiplexer);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GameRepository_GetByInviteCode()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisGameRepository(Multiplexer);
        var game = Game.Create("Test", "INVITE1", DateTimeOffset.UtcNow.AddDays(1), 4, 10);
        await repo.AddAsync(game);

        var retrieved = await repo.GetByInviteCodeAsync("INVITE1");

        Assert.NotNull(retrieved);
        Assert.Equal(game.Id, retrieved.Id);
    }

    [Fact]
    public async Task GameRepository_GetByInviteCode_NonExistent_ReturnsNull()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisGameRepository(Multiplexer);

        var result = await repo.GetByInviteCodeAsync("NONEXIST");

        Assert.Null(result);
    }

    [Fact]
    public async Task GameRepository_Update_ChangesData()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisGameRepository(Multiplexer);
        var game = Game.Create("Original", "CODE1", DateTimeOffset.UtcNow.AddDays(1), 4, 10);
        await repo.AddAsync(game);

        var updatedName = "Updated Name";
        var updatedGame = new Game(
            game.Id, updatedName, game.InviteCode, game.CreatedAt,
            game.ScheduledEndAt, game.MaxPlayers, game.BasePointsPerTag,
            game.ConditionBonuses, game.ConfirmationTimeout, game.SafeTimeBlocks);
        await repo.UpdateAsync(updatedGame);

        var retrieved = await repo.GetByIdAsync(game.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Name", retrieved.Name);
    }

    [Fact]
    public async Task GameRepository_Update_ChangedInviteCode_RemovesOldIndex()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisGameRepository(Multiplexer);
        var game = Game.Create("Test", "OLDCODE", DateTimeOffset.UtcNow.AddDays(1), 4, 10);
        await repo.AddAsync(game);

        var updatedGame = new Game(
            game.Id, game.Name, "NEWCODE", game.CreatedAt,
            game.ScheduledEndAt, game.MaxPlayers, game.BasePointsPerTag,
            game.ConditionBonuses, game.ConfirmationTimeout, game.SafeTimeBlocks);
        await repo.UpdateAsync(updatedGame);

        var byOldCode = await repo.GetByInviteCodeAsync("OLDCODE");
        var byNewCode = await repo.GetByInviteCodeAsync("NEWCODE");

        Assert.Null(byOldCode);
        Assert.NotNull(byNewCode);
        Assert.Equal(game.Id, byNewCode!.Id);
    }

    [Fact]
    public async Task GameRepository_Delete_RemovesGameAndIndex()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisGameRepository(Multiplexer);
        var game = Game.Create("Test", "DELETE", DateTimeOffset.UtcNow.AddDays(1), 4, 10);
        await repo.AddAsync(game);

        await repo.DeleteAsync(game.Id);

        var retrieved = await repo.GetByIdAsync(game.Id);
        var byCode = await repo.GetByInviteCodeAsync("DELETE");
        Assert.Null(retrieved);
        Assert.Null(byCode);
    }

    [Fact]
    public async Task GameRepository_GetAll_ReturnsAllGames()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisGameRepository(Multiplexer);
        var game1 = Game.Create("Game1", "CODE1", DateTimeOffset.UtcNow.AddDays(1), 4, 10);
        var game2 = Game.Create("Game2", "CODE2", DateTimeOffset.UtcNow.AddDays(1), 4, 10);
        await repo.AddAsync(game1);
        await repo.AddAsync(game2);

        var all = await repo.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, g => g.Id == game1.Id);
        Assert.Contains(all, g => g.Id == game2.Id);
    }

    // ── Player Repository ─────────────────────────────────────────────────

    [Fact]
    public async Task PlayerRepository_AddAndGetById()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisPlayerRepository(Multiplexer);
        var player = Player.Create("test@example.com", "TestPlayer");

        await repo.AddAsync(player);
        var retrieved = await repo.GetByIdAsync(player.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(player.Id, retrieved.Id);
        Assert.Equal("test@example.com", retrieved.Email);
        Assert.Equal("TestPlayer", retrieved.DisplayName);
    }

    [Fact]
    public async Task PlayerRepository_GetByEmail()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisPlayerRepository(Multiplexer);
        var player = Player.Create("findme@example.com", "Finder");
        await repo.AddAsync(player);

        var retrieved = await repo.GetByEmailAsync("findme@example.com");

        Assert.NotNull(retrieved);
        Assert.Equal(player.Id, retrieved.Id);
    }

    [Fact]
    public async Task PlayerRepository_GetByEmail_NonExistent_ReturnsNull()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisPlayerRepository(Multiplexer);

        var result = await repo.GetByEmailAsync("nobody@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task PlayerRepository_Update_ChangedEmail_RemovesOldIndex()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisPlayerRepository(Multiplexer);
        var player = Player.Create("old@example.com", "Test");
        await repo.AddAsync(player);

        var updatedPlayer = new Player(player.Id, "new@example.com", player.DisplayName, player.AvatarUrl);
        await repo.UpdateAsync(updatedPlayer);

        var byOldEmail = await repo.GetByEmailAsync("old@example.com");
        var byNewEmail = await repo.GetByEmailAsync("new@example.com");

        Assert.Null(byOldEmail);
        Assert.NotNull(byNewEmail);
        Assert.Equal(player.Id, byNewEmail!.Id);
    }

    [Fact]
    public async Task PlayerRepository_Delete_RemovesPlayerAndEmailIndex()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisPlayerRepository(Multiplexer);
        var player = Player.Create("delete@example.com", "DeleteMe");
        await repo.AddAsync(player);

        await repo.DeleteAsync(player.Id);

        var byId = await repo.GetByIdAsync(player.Id);
        var byEmail = await repo.GetByEmailAsync("delete@example.com");
        Assert.Null(byId);
        Assert.Null(byEmail);
    }

    // ── GamePlayer Repository ─────────────────────────────────────────────

    [Fact]
    public async Task GamePlayerRepository_AddAndGet()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisGamePlayerRepository(Multiplexer);
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var gp = GamePlayer.Create(gameId, playerId, GameRole.Creator);

        await repo.AddAsync(gp);
        var retrieved = await repo.GetAsync(gameId, playerId);

        Assert.NotNull(retrieved);
        Assert.Equal(gameId, retrieved.GameId);
        Assert.Equal(playerId, retrieved.PlayerId);
        Assert.Equal(GameRole.Creator, retrieved.Role);
        Assert.True(retrieved.IsActive);
    }

    [Fact]
    public async Task GamePlayerRepository_GetByGameId()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisGamePlayerRepository(Multiplexer);
        var gameId = Guid.NewGuid();
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        await repo.AddAsync(GamePlayer.Create(gameId, p1));
        await repo.AddAsync(GamePlayer.Create(gameId, p2));

        var members = await repo.GetByGameIdAsync(gameId);

        Assert.Equal(2, members.Count);
    }

    [Fact]
    public async Task GamePlayerRepository_GetByPlayerId()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisGamePlayerRepository(Multiplexer);
        var playerId = Guid.NewGuid();
        var g1 = Guid.NewGuid();
        var g2 = Guid.NewGuid();
        await repo.AddAsync(GamePlayer.Create(g1, playerId));
        await repo.AddAsync(GamePlayer.Create(g2, playerId));

        var games = await repo.GetByPlayerIdAsync(playerId);

        Assert.Equal(2, games.Count);
    }

    [Fact]
    public async Task GamePlayerRepository_Update()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisGamePlayerRepository(Multiplexer);
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var gp = GamePlayer.Create(gameId, playerId);
        await repo.AddAsync(gp);

        gp.AddScore(50);
        await repo.UpdateAsync(gp);
        var retrieved = await repo.GetAsync(gameId, playerId);

        Assert.NotNull(retrieved);
        Assert.Equal(50, retrieved.Score);
    }

    [Fact]
    public async Task GamePlayerRepository_Remove()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisGamePlayerRepository(Multiplexer);
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        await repo.AddAsync(GamePlayer.Create(gameId, playerId));

        await repo.RemoveAsync(gameId, playerId);

        var retrieved = await repo.GetAsync(gameId, playerId);
        var byGame = await repo.GetByGameIdAsync(gameId);
        var byPlayer = await repo.GetByPlayerIdAsync(playerId);

        Assert.Null(retrieved);
        Assert.Empty(byGame);
        Assert.Empty(byPlayer);
    }

    // ── Assignment Repository ─────────────────────────────────────────────

    [Fact]
    public async Task AssignmentRepository_AddAndGetById()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisAssignmentRepository(Multiplexer);
        var conditions = new List<Condition> { AloneCondition.Create() };
        var assignment = Assignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), conditions);

        await repo.AddAsync(assignment);
        var retrieved = await repo.GetByIdAsync(assignment.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(assignment.Id, retrieved.Id);
        Assert.Equal(AssignmentStatus.Active, retrieved.Status);
    }

    [Fact]
    public async Task AssignmentRepository_GetActiveByHunterId()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisAssignmentRepository(Multiplexer);
        var gameId = Guid.NewGuid();
        var hunterId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var conditions = new List<Condition> { AloneCondition.Create() };
        var assignment = Assignment.Create(gameId, hunterId, targetId, conditions);
        await repo.AddAsync(assignment);

        var retrieved = await repo.GetActiveByHunterIdAsync(gameId, hunterId);

        Assert.NotNull(retrieved);
        Assert.Equal(assignment.Id, retrieved.Id);
    }

    [Fact]
    public async Task AssignmentRepository_GetActiveByHunterId_NotActive_ReturnsNull()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisAssignmentRepository(Multiplexer);
        var gameId = Guid.NewGuid();
        var hunterId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var conditions = new List<Condition> { AloneCondition.Create() };
        var assignment = Assignment.Create(gameId, hunterId, targetId, conditions);
        await repo.AddAsync(assignment);

        assignment.Complete();
        await repo.UpdateAsync(assignment);

        var retrieved = await repo.GetActiveByHunterIdAsync(gameId, hunterId);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task AssignmentRepository_GetByGameId()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisAssignmentRepository(Multiplexer);
        var gameId = Guid.NewGuid();
        var a1 = Assignment.Create(gameId, Guid.NewGuid(), Guid.NewGuid(), [AloneCondition.Create()]);
        var a2 = Assignment.Create(gameId, Guid.NewGuid(), Guid.NewGuid(), [AloneCondition.Create()]);
        await repo.AddAsync(a1);
        await repo.AddAsync(a2);

        var assignments = await repo.GetByGameIdAsync(gameId);

        Assert.Equal(2, assignments.Count);
    }

    [Fact]
    public async Task AssignmentRepository_Update_CompletedAssignment_RemovesActiveIndex()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisAssignmentRepository(Multiplexer);
        var gameId = Guid.NewGuid();
        var hunterId = Guid.NewGuid();
        var assignment = Assignment.Create(gameId, hunterId, Guid.NewGuid(), [AloneCondition.Create()]);
        await repo.AddAsync(assignment);

        assignment.Complete();
        await repo.UpdateAsync(assignment);

        var byHunter = await repo.GetActiveByHunterIdAsync(gameId, hunterId);
        Assert.Null(byHunter);
    }

    [Fact]
    public async Task AssignmentRepository_Update_ChangedGameId_MovesGameIndex()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisAssignmentRepository(Multiplexer);
        var gameId1 = Guid.NewGuid();
        var gameId2 = Guid.NewGuid();
        var assignment = Assignment.Create(gameId1, Guid.NewGuid(), Guid.NewGuid(), [AloneCondition.Create()]);
        await repo.AddAsync(assignment);

        // Simulate game ID change via JSON constructor
        var changed = new Assignment(
            assignment.Id, gameId2, assignment.HunterId, assignment.TargetId,
            assignment.AssignedAt, assignment.Conditions);
        await repo.UpdateAsync(changed);

        var byGame1 = await repo.GetByGameIdAsync(gameId1);
        var byGame2 = await repo.GetByGameIdAsync(gameId2);

        Assert.Empty(byGame1);
        Assert.Single(byGame2);
    }

    // ── TagSubmission Repository ──────────────────────────────────────────

    [Fact]
    public async Task TagSubmissionRepository_AddAndGetById()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisTagSubmissionRepository(Multiplexer);
        var submission = TagSubmission.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await repo.AddAsync(submission);
        var retrieved = await repo.GetByIdAsync(submission.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(submission.Id, retrieved.Id);
        Assert.Equal(TagStatus.Pending, retrieved.Status);
    }

    [Fact]
    public async Task TagSubmissionRepository_GetPendingByTargetId()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisTagSubmissionRepository(Multiplexer);
        var targetId = Guid.NewGuid();
        var submission = TagSubmission.Create(Guid.NewGuid(), Guid.NewGuid(), targetId, Guid.NewGuid());
        await repo.AddAsync(submission);

        var pending = await repo.GetPendingByTargetIdAsync(targetId);

        Assert.Single(pending);
        Assert.Equal(submission.Id, pending[0].Id);
    }

    [Fact]
    public async Task TagSubmissionRepository_GetPendingByTargetId_ConfirmedNotIncluded()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisTagSubmissionRepository(Multiplexer);
        var targetId = Guid.NewGuid();
        var submission = TagSubmission.Create(Guid.NewGuid(), Guid.NewGuid(), targetId, Guid.NewGuid());
        await repo.AddAsync(submission);
        submission.Confirm();
        await repo.UpdateAsync(submission);

        var pending = await repo.GetPendingByTargetIdAsync(targetId);

        Assert.Empty(pending);
    }

    [Fact]
    public async Task TagSubmissionRepository_Update_ResolvedSubmission_RemovesPendingIndex()
    {
        SkipIfRedisUnavailable();
        var repo = new RedisTagSubmissionRepository(Multiplexer);
        var targetId = Guid.NewGuid();
        var submission = TagSubmission.Create(Guid.NewGuid(), Guid.NewGuid(), targetId, Guid.NewGuid());
        await repo.AddAsync(submission);

        submission.Confirm();
        await repo.UpdateAsync(submission);

        var pending = await repo.GetPendingByTargetIdAsync(targetId);
        Assert.Empty(pending);
    }

    // ── Condition Library ─────────────────────────────────────────────────

    [Fact]
    public async Task ConditionLibrary_Get_ReturnsDefaultConditionsWhenEmpty()
    {
        SkipIfRedisUnavailable();
        var library = new RedisConditionLibrary(Multiplexer);
        var gameId = Guid.NewGuid();

        var conditions = await library.GetAsync(gameId);

        Assert.NotEmpty(conditions);
        Assert.Equal(4, conditions.Count);
        Assert.Contains(conditions, c => c is WithSpecificPersonCondition);
        Assert.Contains(conditions, c => c is AloneCondition);
        Assert.Contains(conditions, c => c is WithXPeopleCondition);
        Assert.Contains(conditions, c => c is MundaneActionCondition);
    }

    [Fact]
    public async Task ConditionLibrary_Get_ReturnsPersistedConditions()
    {
        SkipIfRedisUnavailable();
        var library = new RedisConditionLibrary(Multiplexer);
        var gameId = Guid.NewGuid();

        // First call persists defaults
        await library.GetAsync(gameId);

        // Second call should return the persisted defaults
        var conditions = await library.GetAsync(gameId);

        Assert.Equal(4, conditions.Count);
    }

    [Fact]
    public async Task ConditionLibrary_Add_AddsConditionToExisting()
    {
        SkipIfRedisUnavailable();
        var library = new RedisConditionLibrary(Multiplexer);
        var gameId = Guid.NewGuid();

        await library.AddAsync(gameId, CustomCondition.Create("Extra condition"));
        var conditions = await library.GetAsync(gameId);

        Assert.Contains(conditions, c => c is CustomCondition custom && custom.Description == "Extra condition");
    }
}
