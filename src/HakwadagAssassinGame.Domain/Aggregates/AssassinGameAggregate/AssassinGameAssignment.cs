namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class AssassinGameAssignment : TimeStampedEntity
{
    private readonly List<AssassinGameAssignmentTask> _tasks = [];

    public AssassinGameAssignment(string playerUserId, string targetUserId, AssassinGameAssignmentType assignmentType,
        IEnumerable<AssassinGameAssignmentTask> tasks)
    {
        PlayerUserId = playerUserId;
        TargetUserId = targetUserId;
        AssignmentType = assignmentType;
        _tasks.AddRange(tasks);
    }

    public string PlayerUserId { get; }
    public string TargetUserId { get; }
    public AssassinGameAssignmentType AssignmentType { get; private set; }
    public IReadOnlyList<AssassinGameAssignmentTask> Tasks => _tasks;
}