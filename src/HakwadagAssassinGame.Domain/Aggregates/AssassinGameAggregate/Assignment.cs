namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class Assignment : TimeStampedEntity
{
    private readonly List<AssignmentTask> _tasks = [];

    private Assignment() {}
    
    public Assignment(Guid playerUserId, Guid targetUserId, AssignmentType assignmentType,
        IEnumerable<AssignmentTask> tasks)
    {
        PlayerUserId = playerUserId;
        TargetUserId = targetUserId;
        AssignmentTypeId = assignmentType.Id;
        _tasks.AddRange(tasks);
    }

    public Guid? PlayerUserId { get; }
    public Guid? TargetUserId { get; }
    public int AssignmentTypeId { get; }
    public AssignmentType AssignmentType => AssignmentType.FromId(AssignmentTypeId);
    public IReadOnlyList<AssignmentTask> Tasks => _tasks;
}