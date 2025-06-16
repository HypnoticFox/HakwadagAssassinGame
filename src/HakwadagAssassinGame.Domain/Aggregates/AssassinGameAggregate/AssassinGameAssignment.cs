namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class AssassinGameAssignment : TimeStampedEntity
{
    private readonly List<AssassinGameAssignmentTask> _tasks = [];

    public AssassinGameAssignment(string playerUserId, string targetUserId, AssassinGameAssignmentType assignmentType,
        IEnumerable<AssassinGameAssignmentTask> tasks)
    {
        PlayerUserId = playerUserId;
        TargetUserId = targetUserId;
        AssignmentTypeId = assignmentType.Id;
        _tasks.AddRange(tasks);
    }

    public string PlayerUserId { get; }
    public string TargetUserId { get; }
    public int AssignmentTypeId { get; }
    public AssassinGameAssignmentType AssignmentType => AssassinGameAssignmentType.FromId(AssignmentTypeId);
    public IReadOnlyList<AssassinGameAssignmentTask> Tasks => _tasks;
}