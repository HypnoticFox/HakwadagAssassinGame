using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Enums;

namespace HakwadagAssassinGame.Tests.Unit.Core;

public sealed class TagSubmissionTests
{
    private static readonly Guid AssignmentId = Guid.NewGuid();
    private static readonly Guid HunterId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();
    private static readonly Guid ConditionId = Guid.NewGuid();

    // ── Create validation ──────────────────────────────────────────────────

    [Fact]
    public void Create_EmptyAssignmentId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            TagSubmission.Create(Guid.Empty, HunterId, TargetId, ConditionId));
        Assert.Contains("assignmentId", ex.Message);
    }

    [Fact]
    public void Create_EmptyHunterId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            TagSubmission.Create(AssignmentId, Guid.Empty, TargetId, ConditionId));
        Assert.Contains("hunterId", ex.Message);
    }

    [Fact]
    public void Create_EmptyTargetId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            TagSubmission.Create(AssignmentId, HunterId, Guid.Empty, ConditionId));
        Assert.Contains("targetId", ex.Message);
    }

    [Fact]
    public void Create_EmptyConditionId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            TagSubmission.Create(AssignmentId, HunterId, TargetId, Guid.Empty));
        Assert.Contains("conditionId", ex.Message);
    }

    [Fact]
    public void Create_ValidInputs_SetsPropertiesCorrectly()
    {
        var submissionId = Guid.NewGuid();
        var submittedAt = DateTimeOffset.UtcNow;

        var submission = TagSubmission.Create(
            AssignmentId, HunterId, TargetId, ConditionId,
            id: submissionId, submittedAt: submittedAt);

        Assert.Equal(submissionId, submission.Id);
        Assert.Equal(AssignmentId, submission.AssignmentId);
        Assert.Equal(HunterId, submission.HunterId);
        Assert.Equal(TargetId, submission.TargetId);
        Assert.Equal(ConditionId, submission.ConditionId);
        Assert.Equal(TagStatus.Pending, submission.Status);
        Assert.Equal(submittedAt, submission.SubmittedAt);
        Assert.Null(submission.ResolvedAt);
    }

    [Fact]
    public void Create_DefaultStatusIsPending()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        Assert.Equal(TagStatus.Pending, submission.Status);
    }

    [Fact]
    public void Create_DefaultId_IsNotEmpty()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        Assert.NotEqual(Guid.Empty, submission.Id);
    }

    [Fact]
    public void Create_DefaultSubmittedAt_IsUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        var after = DateTimeOffset.UtcNow;
        Assert.InRange(submission.SubmittedAt, before, after);
    }

    // ── Confirm ────────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_Pending_SetsConfirmedAndResolvedAt()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        var resolvedAt = DateTimeOffset.UtcNow.AddHours(1);

        submission.Confirm(resolvedAt);

        Assert.Equal(TagStatus.Confirmed, submission.Status);
        Assert.Equal(resolvedAt, submission.ResolvedAt);
    }

    [Fact]
    public void Confirm_Pending_NoResolvedAtProvided_UsesUtcNow()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        var before = DateTimeOffset.UtcNow;

        submission.Confirm();

        var after = DateTimeOffset.UtcNow;
        Assert.Equal(TagStatus.Confirmed, submission.Status);
        Assert.NotNull(submission.ResolvedAt);
        Assert.InRange(submission.ResolvedAt!.Value, before, after);
    }

    [Fact]
    public void Confirm_Confirmed_ThrowsInvalidOperationException()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        submission.Confirm();

        var ex = Assert.Throws<InvalidOperationException>(() => submission.Confirm());
        Assert.Contains("pending tag", ex.Message);
    }

    [Fact]
    public void Confirm_Denied_ThrowsInvalidOperationException()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        submission.Deny();

        var ex = Assert.Throws<InvalidOperationException>(() => submission.Confirm());
        Assert.Contains("pending tag", ex.Message);
    }

    [Fact]
    public void Confirm_Voided_ThrowsInvalidOperationException()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        submission.Void();

        var ex = Assert.Throws<InvalidOperationException>(() => submission.Confirm());
        Assert.Contains("pending tag", ex.Message);
    }

    // ── Deny ───────────────────────────────────────────────────────────────

    [Fact]
    public void Deny_Pending_SetsDeniedAndResolvedAt()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        var resolvedAt = DateTimeOffset.UtcNow.AddHours(1);

        submission.Deny(resolvedAt);

        Assert.Equal(TagStatus.Denied, submission.Status);
        Assert.Equal(resolvedAt, submission.ResolvedAt);
    }

    [Fact]
    public void Deny_Pending_NoResolvedAtProvided_UsesUtcNow()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        var before = DateTimeOffset.UtcNow;

        submission.Deny();

        var after = DateTimeOffset.UtcNow;
        Assert.Equal(TagStatus.Denied, submission.Status);
        Assert.NotNull(submission.ResolvedAt);
        Assert.InRange(submission.ResolvedAt!.Value, before, after);
    }

    [Fact]
    public void Deny_Confirmed_ThrowsInvalidOperationException()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        submission.Confirm();

        var ex = Assert.Throws<InvalidOperationException>(() => submission.Deny());
        Assert.Contains("pending tag", ex.Message);
    }

    [Fact]
    public void Deny_Denied_ThrowsInvalidOperationException()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        submission.Deny();

        var ex = Assert.Throws<InvalidOperationException>(() => submission.Deny());
        Assert.Contains("pending tag", ex.Message);
    }

    [Fact]
    public void Deny_Voided_ThrowsInvalidOperationException()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        submission.Void();

        var ex = Assert.Throws<InvalidOperationException>(() => submission.Deny());
        Assert.Contains("pending tag", ex.Message);
    }

    // ── Void ───────────────────────────────────────────────────────────────

    [Fact]
    public void Void_Pending_SetsVoidedAndResolvedAt()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        var resolvedAt = DateTimeOffset.UtcNow.AddHours(1);

        submission.Void(resolvedAt);

        Assert.Equal(TagStatus.Voided, submission.Status);
        Assert.Equal(resolvedAt, submission.ResolvedAt);
    }

    [Fact]
    public void Void_Pending_NoResolvedAtProvided_UsesUtcNow()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        var before = DateTimeOffset.UtcNow;

        submission.Void();

        var after = DateTimeOffset.UtcNow;
        Assert.Equal(TagStatus.Voided, submission.Status);
        Assert.NotNull(submission.ResolvedAt);
        Assert.InRange(submission.ResolvedAt!.Value, before, after);
    }

    [Fact]
    public void Void_Confirmed_ThrowsInvalidOperationException()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        submission.Confirm();

        var ex = Assert.Throws<InvalidOperationException>(() => submission.Void());
        Assert.Contains("pending tag", ex.Message);
    }

    [Fact]
    public void Void_Denied_ThrowsInvalidOperationException()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        submission.Deny();

        var ex = Assert.Throws<InvalidOperationException>(() => submission.Void());
        Assert.Contains("pending tag", ex.Message);
    }

    [Fact]
    public void Void_Voided_ThrowsInvalidOperationException()
    {
        var submission = TagSubmission.Create(AssignmentId, HunterId, TargetId, ConditionId);
        submission.Void();

        var ex = Assert.Throws<InvalidOperationException>(() => submission.Void());
        Assert.Contains("pending tag", ex.Message);
    }

    // ── JSON Constructor ───────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithJsonConstructor_SetsStatusToPending()
    {
        var submission = new TagSubmission(
            Guid.NewGuid(), AssignmentId, HunterId, TargetId, ConditionId,
            DateTimeOffset.UtcNow);
        Assert.Equal(TagStatus.Pending, submission.Status);
        Assert.Null(submission.ResolvedAt);
    }
}
