using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;
using NSubstitute;

namespace HakwadagAssassinGame.Tests.Unit.Application;

public sealed class ServiceHelpersTests
{
    // ── CreateDerangement ──────────────────────────────────────────────────

    [Fact]
    public void CreateDerangement_Valid_ProducesDerangement()
    {
        var memberships = new List<GamePlayer>
        {
            GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid()),
            GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid()),
            GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid()),
            GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid()),
        };
        // All active by default

        var result = ServiceHelpers.CreateDerangement(memberships);

        Assert.Equal(memberships.Count, result.Count);
        // No element should be at its original position
        for (var i = 0; i < memberships.Count; i++)
        {
            Assert.NotEqual(memberships[i].PlayerId, result[i]);
        }
    }

    [Fact]
    public void CreateDerangement_IgnoresInactiveMembers()
    {
        var active1 = GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid());
        var active2 = GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid());
        var active3 = GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid());
        var inactive = GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid());
        inactive.Deactivate();

        var memberships = new List<GamePlayer> { active1, inactive, active2, active3 };
        var result = ServiceHelpers.CreateDerangement(memberships);

        Assert.Equal(3, result.Count);
        Assert.NotEqual(active1.PlayerId, result[0]);
        Assert.NotEqual(active2.PlayerId, result[1]);
        Assert.NotEqual(active3.PlayerId, result[2]);
        Assert.DoesNotContain(inactive.PlayerId, result);
    }

    [Fact]
    public void CreateDerangement_LessThan3Active_ThrowsInvalidGameStateException()
    {
        var memberships = new List<GamePlayer>
        {
            GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid()),
            GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid()),
        };

        var ex = Assert.Throws<InvalidGameStateException>(() =>
            ServiceHelpers.CreateDerangement(memberships));
        Assert.Contains("at least three", ex.Message.ToLower());
    }

    [Fact]
    public void CreateDerangement_EmptyList_ThrowsInvalidGameStateException()
    {
        var ex = Assert.Throws<InvalidGameStateException>(() =>
            ServiceHelpers.CreateDerangement(new List<GamePlayer>()));
        Assert.Contains("at least three", ex.Message.ToLower());
    }

    [Fact]
    public void CreateDerangement_ThreePlayers_ProducesDerangement()
    {
        var p1 = GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid());
        var p2 = GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid());
        var p3 = GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid());
        var memberships = new List<GamePlayer> { p1, p2, p3 };

        var result = ServiceHelpers.CreateDerangement(memberships);

        Assert.Equal(3, result.Count);
        Assert.NotEqual(p1.PlayerId, result[0]);
        Assert.NotEqual(p2.PlayerId, result[1]);
        Assert.NotEqual(p3.PlayerId, result[2]);
    }

    // ── IsAdmin ────────────────────────────────────────────────────────────

    [Fact]
    public void IsAdmin_Creator_ReturnsTrue()
    {
        var membership = GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid(), GameRole.Creator);
        Assert.True(ServiceHelpers.IsAdmin(membership));
    }

    [Fact]
    public void IsAdmin_CoAdmin_ReturnsTrue()
    {
        var membership = GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid(), GameRole.CoAdmin);
        Assert.True(ServiceHelpers.IsAdmin(membership));
    }

    [Fact]
    public void IsAdmin_Player_ReturnsFalse()
    {
        var membership = GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid(), GameRole.Player);
        Assert.False(ServiceHelpers.IsAdmin(membership));
    }

    // ── CloneCondition ─────────────────────────────────────────────────────

    [Fact]
    public void CloneCondition_AloneCondition_ClonesCorrectly()
    {
        var original = AloneCondition.Create();
        var cloned = ServiceHelpers.CloneCondition(original, Array.Empty<Player>());

        Assert.IsType<AloneCondition>(cloned);
        Assert.NotEqual(original.Id, cloned.Id);
        Assert.Equal(ConditionType.Alone, cloned.Type);
    }

    [Fact]
    public void CloneCondition_WithSpecificPersonCondition_ClonesCorrectly()
    {
        var otherPlayer = Player.Create("other@test.com", "Other", id: Guid.NewGuid());
        var original = WithSpecificPersonCondition.Create(Guid.NewGuid());
        var players = new[] { otherPlayer, Player.Create("another@test.com", "Another", id: Guid.NewGuid()) };

        var cloned = ServiceHelpers.CloneCondition(original, players);

        Assert.IsType<WithSpecificPersonCondition>(cloned);
        Assert.NotEqual(original.Id, cloned.Id);
        Assert.Equal(ConditionType.WithSpecificPerson, cloned.Type);
        var typedCloned = (WithSpecificPersonCondition)cloned;
        Assert.NotNull(typedCloned.TargetPersonId);
        // Should pick one of the other players
        Assert.Contains(typedCloned.TargetPersonId!.Value, players.Select(p => p.Id));
    }

    [Fact]
    public void CloneCondition_WithSpecificPersonCondition_NoOtherPlayers_SetsTargetPersonIdNull()
    {
        var original = WithSpecificPersonCondition.Create(Guid.NewGuid());

        var cloned = ServiceHelpers.CloneCondition(original, Array.Empty<Player>());

        var typedCloned = (WithSpecificPersonCondition)cloned;
        Assert.Null(typedCloned.TargetPersonId);
    }

    [Fact]
    public void CloneCondition_WithXPeopleCondition_ClonesCorrectly()
    {
        var original = WithXPeopleCondition.Create(3);
        var cloned = ServiceHelpers.CloneCondition(original, Array.Empty<Player>());

        Assert.IsType<WithXPeopleCondition>(cloned);
        Assert.NotEqual(original.Id, cloned.Id);
        Assert.Equal(ConditionType.WithXPeople, cloned.Type);
        Assert.Equal(3, ((WithXPeopleCondition)cloned).MinPeople);
    }

    [Fact]
    public void CloneCondition_MundaneActionCondition_ClonesCorrectly()
    {
        var original = MundaneActionCondition.Create("eating");
        var cloned = ServiceHelpers.CloneCondition(original, Array.Empty<Player>());

        Assert.IsType<MundaneActionCondition>(cloned);
        Assert.NotEqual(original.Id, cloned.Id);
        Assert.Equal(ConditionType.MundaneAction, cloned.Type);
        Assert.NotNull(((MundaneActionCondition)cloned).Action);
    }

    [Fact]
    public void CloneCondition_CustomCondition_ClonesCorrectly()
    {
        var original = CustomCondition.Create("Must be holding a coffee cup");
        var cloned = ServiceHelpers.CloneCondition(original, Array.Empty<Player>());

        Assert.IsType<CustomCondition>(cloned);
        Assert.NotEqual(original.Id, cloned.Id);
        Assert.Equal(ConditionType.Custom, cloned.Type);
        Assert.Equal("Must be holding a coffee cup", ((CustomCondition)cloned).Description);
    }

    [Fact]
    public void CloneCondition_UnknownConditionType_DefaultsToAlone()
    {
        var unknown = new TestCondition();
        var cloned = ServiceHelpers.CloneCondition(unknown, Array.Empty<Player>());

        Assert.IsType<AloneCondition>(cloned);
    }

    /// <summary>A test-only condition type not registered in CloneCondition's switch.</summary>
    private sealed class TestCondition : Condition
    {
        public TestCondition() : base((ConditionType)999) { }
        public override string Describe() => "test";
    }

    // ── CreateConditions ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateConditions_EmptyLibrary_DefaultsToAloneCondition()
    {
        var library = Substitute.For<IConditionLibrary>();
        library.GetAsync(Guid.NewGuid(), Arg.Any<CancellationToken>())
            .Returns(new List<Condition>());

        var player = Player.Create("player@test.com", "Player", id: Guid.NewGuid());
        var result = await ServiceHelpers.CreateConditions(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new[] { player }, library, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.IsType<AloneCondition>(result[0]);
    }

    [Fact]
    public async Task CreateConditions_WithTemplates_SelectsConditions()
    {
        var library = Substitute.For<IConditionLibrary>();
        var templates = new List<Condition>
        {
            AloneCondition.Create(),
            WithXPeopleCondition.Create(2),
            MundaneActionCondition.Create("eating"),
            CustomCondition.Create("Custom"),
        };
        library.GetAsync(Guid.NewGuid(), Arg.Any<CancellationToken>()).Returns(templates);

        var player = Player.Create("player@test.com", "Player", id: Guid.NewGuid());
        var result = await ServiceHelpers.CreateConditions(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new[] { player }, library, CancellationToken.None);

        Assert.NotNull(result);
        // Should select between 2 and 3 (but limited by templates to max 3)
        Assert.InRange(result.Count, 1, 3);
    }

    [Fact]
    public async Task CreateConditions_WithFewerTemplatesThanDesired_SelectsAll()
    {
        var library = Substitute.For<IConditionLibrary>();
        var templates = new List<Condition>
        {
            AloneCondition.Create(),
        };
        library.GetAsync(Guid.NewGuid(), Arg.Any<CancellationToken>()).Returns(templates);

        var player = Player.Create("player@test.com", "Player", id: Guid.NewGuid());
        var result = await ServiceHelpers.CreateConditions(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new[] { player }, library, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.IsType<AloneCondition>(result[0]);
    }

    [Fact]
    public async Task CreateConditions_WithSpecificPerson_ExcludesHunterAndTarget()
    {
        var gameId = Guid.NewGuid();
        var hunterId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var bystander1 = Player.Create("b1@test.com", "Bystander1", id: Guid.NewGuid());
        var bystander2 = Player.Create("b2@test.com", "Bystander2", id: Guid.NewGuid());
        var hunter = Player.Create("hunter@test.com", "Hunter", id: hunterId);
        var target = Player.Create("target@test.com", "Target", id: targetId);
        var players = new[] { hunter, target, bystander1, bystander2 };

        var library = Substitute.For<IConditionLibrary>();
        var templates = new List<Condition> { WithSpecificPersonCondition.Create(null) };
        library.GetAsync(gameId, Arg.Any<CancellationToken>()).Returns(templates);

        // Run many times to be confident — random selection should never pick hunter or target.
        for (var i = 0; i < 50; i++)
        {
            var result = await ServiceHelpers.CreateConditions(
                gameId, hunterId, targetId, players, library, CancellationToken.None);

            var specific = Assert.IsType<WithSpecificPersonCondition>(Assert.Single(result));
            Assert.NotNull(specific.TargetPersonId);
            Assert.NotEqual(hunterId, specific.TargetPersonId.Value);
            Assert.NotEqual(targetId, specific.TargetPersonId.Value);
        }
    }

    // ── RequirePlayerAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task RequirePlayerAsync_PlayerExists_ReturnsPlayer()
    {
        var playerId = Guid.NewGuid();
        var expected = Player.Create("test@test.com", "Test", id: playerId);
        var repository = Substitute.For<IPlayerRepository>();
        repository.GetByIdAsync(playerId, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await ServiceHelpers.RequirePlayerAsync(repository, playerId, CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task RequirePlayerAsync_PlayerNotFound_ThrowsPlayerNotFoundException()
    {
        var playerId = Guid.NewGuid();
        var repository = Substitute.For<IPlayerRepository>();
        repository.GetByIdAsync(playerId, Arg.Any<CancellationToken>()).Returns((Player?)null);

        await Assert.ThrowsAsync<PlayerNotFoundException>(() =>
            ServiceHelpers.RequirePlayerAsync(repository, playerId, CancellationToken.None));
    }

    // ── RequireGameAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task RequireGameAsync_GameExists_ReturnsGame()
    {
        var gameId = Guid.NewGuid();
        var expected = Game.Create("Test", "CODE", DateTimeOffset.UtcNow.AddDays(1), 4, 10);
        var repository = Substitute.For<IGameRepository>();
        repository.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await ServiceHelpers.RequireGameAsync(repository, gameId, CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task RequireGameAsync_GameNotFound_ThrowsGameNotFoundException()
    {
        var gameId = Guid.NewGuid();
        var repository = Substitute.For<IGameRepository>();
        repository.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns((Game?)null);

        await Assert.ThrowsAsync<GameNotFoundException>(() =>
            ServiceHelpers.RequireGameAsync(repository, gameId, CancellationToken.None));
    }

    // ── RequireMembershipAsync ─────────────────────────────────────────────

    [Fact]
    public async Task RequireMembershipAsync_MembershipExists_ReturnsMembership()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var expected = GamePlayer.Create(gameId, playerId);
        var repository = Substitute.For<IGamePlayerRepository>();
        repository.GetAsync(gameId, playerId, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await ServiceHelpers.RequireMembershipAsync(repository, gameId, playerId, CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task RequireMembershipAsync_MembershipNotFound_ThrowsUnauthorizedException()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var repository = Substitute.For<IGamePlayerRepository>();
        repository.GetAsync(gameId, playerId, Arg.Any<CancellationToken>()).Returns((GamePlayer?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            ServiceHelpers.RequireMembershipAsync(repository, gameId, playerId, CancellationToken.None));
    }

    // ── MapAssignment ──────────────────────────────────────────────────────

    [Fact]
    public void MapAssignment_Valid_ReturnsAssignmentDto()
    {
        var target = Player.Create("target@test.com", "Target", id: Guid.NewGuid());
        var assignment = Assignment.Create(
            Guid.NewGuid(), Guid.NewGuid(), target.Id,
            new List<Condition>
            {
                AloneCondition.Create(),
                MundaneActionCondition.Create("eating"),
            });

        var players = new List<Player> { target };
        var result = ServiceHelpers.MapAssignment(assignment, target, players);

        Assert.NotNull(result);
        Assert.Equal(assignment.Id, result.Id);
        Assert.Equal(target.Id, result.Target.Id);
        Assert.Equal(2, result.Conditions.Count);
    }

    // ── MapTag ─────────────────────────────────────────────────────────────

    [Fact]
    public void MapTag_Valid_ReturnsTagSubmissionDto()
    {
        var submission = TagSubmission.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var result = ServiceHelpers.MapTag(submission);

        Assert.NotNull(result);
        Assert.Equal(submission.Id, result.Id);
        Assert.Equal(TagStatus.Pending, result.Status);
        Assert.Equal(submission.SubmittedAt, result.SubmittedAt);
    }
}
