namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class AssignmentTask : TimeStampedEntity
{
    public int AssignmentId { get; } = 0;
    public required int Cost { get; init; }
    public required int Reward { get; init; }
    public required string Description { get; init; }
}