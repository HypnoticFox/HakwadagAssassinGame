using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Tests.Unit.Core;

public sealed class AssignmentTests
{
    private static readonly Guid GameId = Guid.NewGuid();
    private static readonly Guid HunterId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();
    private static readonly List<Condition> DefaultConditions =
        [AloneCondition.Create()];

    // ── Create validation ──────────────────────────────────────────────────

    [Fact]
    public void Create_EmptyGameId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Assignment.Create(Guid.Empty, HunterId, TargetId, DefaultConditions));
        Assert.Contains("gameId", ex.Message);
    }

    [Fact]
    public void Create_EmptyHunterId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Assignment.Create(GameId, Guid.Empty, TargetId, DefaultConditions));
        Assert.Contains("hunterId", ex.Message);
    }

    [Fact]
    public void Create_EmptyTargetId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Assignment.Create(GameId, HunterId, Guid.Empty, DefaultConditions));
        Assert.Contains("targetId", ex.Message);
    }

    [Fact]
    public void Create_HunterEqualsTarget_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Assignment.Create(GameId, HunterId, HunterId, DefaultConditions));
        Assert.Contains("targetId", ex.Message);
    }

    [Fact]
    public void Create_NullConditions_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Assignment.Create(GameId, HunterId, TargetId, null!));
        // LINQ's ToList() on null throws with parameter name "source"
        Assert.Contains("source", ex.Message);
    }

    [Fact]
    public void Create_EmptyConditionsList_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Assignment.Create(GameId, HunterId, TargetId, new List<Condition>()));
        Assert.Contains("conditions", ex.Message);
    }

    [Fact]
    public void Create_ValidInputs_SetsPropertiesCorrectly()
    {
        var assignmentId = Guid.NewGuid();
        var assignedAt = DateTimeOffset.UtcNow;
        var conditions = new List<Condition>
        {
            AloneCondition.Create(),
            MundaneActionCondition.Create("Eating a banana")
        };

        var assignment = Assignment.Create(
            GameId, HunterId, TargetId, conditions,
            id: assignmentId, assignedAt: assignedAt);

        Assert.Equal(assignmentId, assignment.Id);
        Assert.Equal(GameId, assignment.GameId);
        Assert.Equal(HunterId, assignment.HunterId);
        Assert.Equal(TargetId, assignment.TargetId);
        Assert.Equal(AssignmentStatus.Active, assignment.Status);
        Assert.Equal(assignedAt, assignment.AssignedAt);
        Assert.Equal(2, assignment.Conditions.Count);
    }

    [Fact]
    public void Create_DefaultId_IsNotEmpty()
    {
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        Assert.NotEqual(Guid.Empty, assignment.Id);
    }

    [Fact]
    public void Create_DefaultAssignedAt_IsUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        var after = DateTimeOffset.UtcNow;
        Assert.InRange(assignment.AssignedAt, before, after);
    }

    // ── Complete ───────────────────────────────────────────────────────────

    [Fact]
    public void Complete_Active_TransitionsToCompleted()
    {
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        assignment.Complete();
        Assert.Equal(AssignmentStatus.Completed, assignment.Status);
    }

    [Fact]
    public void Complete_Completed_ThrowsInvalidOperationException()
    {
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        assignment.Complete();

        var ex = Assert.Throws<InvalidOperationException>(() => assignment.Complete());
        Assert.Contains("active assignment", ex.Message);
    }

    [Fact]
    public void Complete_Voided_ThrowsInvalidOperationException()
    {
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        assignment.Void();

        var ex = Assert.Throws<InvalidOperationException>(() => assignment.Complete());
        Assert.Contains("active assignment", ex.Message);
    }

    [Fact]
    public void Complete_TargetLeft_ThrowsInvalidOperationException()
    {
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        assignment.MarkTargetLeft();

        var ex = Assert.Throws<InvalidOperationException>(() => assignment.Complete());
        Assert.Contains("active assignment", ex.Message);
    }

    // ── Void ───────────────────────────────────────────────────────────────

    [Fact]
    public void Void_Active_TransitionsToVoided()
    {
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        assignment.Void();
        Assert.Equal(AssignmentStatus.Voided, assignment.Status);
    }

    [Fact]
    public void Void_Completed_ThrowsInvalidOperationException()
    {
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        assignment.Complete();

        var ex = Assert.Throws<InvalidOperationException>(() => assignment.Void());
        Assert.Contains("active assignment", ex.Message);
    }

    [Fact]
    public void Void_Voided_ThrowsInvalidOperationException()
    {
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        assignment.Void();

        var ex = Assert.Throws<InvalidOperationException>(() => assignment.Void());
        Assert.Contains("active assignment", ex.Message);
    }

    [Fact]
    public void Void_TargetLeft_ThrowsInvalidOperationException()
    {
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        assignment.MarkTargetLeft();

        var ex = Assert.Throws<InvalidOperationException>(() => assignment.Void());
        Assert.Contains("active assignment", ex.Message);
    }

    // ── MarkTargetLeft ─────────────────────────────────────────────────────

    [Fact]
    public void MarkTargetLeft_Active_TransitionsToTargetLeft()
    {
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        assignment.MarkTargetLeft();
        Assert.Equal(AssignmentStatus.TargetLeft, assignment.Status);
    }

    [Fact]
    public void MarkTargetLeft_Completed_ThrowsInvalidOperationException()
    {
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        assignment.Complete();

        var ex = Assert.Throws<InvalidOperationException>(() => assignment.MarkTargetLeft());
        Assert.Contains("active assignment", ex.Message);
    }

    [Fact]
    public void MarkTargetLeft_Voided_ThrowsInvalidOperationException()
    {
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        assignment.Void();

        var ex = Assert.Throws<InvalidOperationException>(() => assignment.MarkTargetLeft());
        Assert.Contains("active assignment", ex.Message);
    }

    [Fact]
    public void MarkTargetLeft_TargetLeft_ThrowsInvalidOperationException()
    {
        var assignment = Assignment.Create(GameId, HunterId, TargetId, DefaultConditions);
        assignment.MarkTargetLeft();

        var ex = Assert.Throws<InvalidOperationException>(() => assignment.MarkTargetLeft());
        Assert.Contains("active assignment", ex.Message);
    }

    // ── JSON Constructor ───────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithJsonConstructor_SetsStatusToActive()
    {
        var assignment = new Assignment(
            Guid.NewGuid(), GameId, HunterId, TargetId,
            DateTimeOffset.UtcNow, DefaultConditions);
        Assert.Equal(AssignmentStatus.Active, assignment.Status);
    }

    [Fact]
    public void Constructor_JsonEmptyConditions_ThrowsArgumentNullException()
    {
        // For JSON deserialization, the constructor should throw
        // if conditions is null or empty.
        Assert.Throws<ArgumentNullException>(() =>
            new Assignment(Guid.NewGuid(), GameId, HunterId, TargetId,
                DateTimeOffset.UtcNow, null!));
    }
}
