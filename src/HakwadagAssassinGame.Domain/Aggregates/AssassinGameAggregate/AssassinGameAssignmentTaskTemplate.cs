namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class AssassinGameAssignmentTaskTemplate : Entity
{
    public required int Cost { get; init; }
    public required int Reward { get; init; }
    public required string Description { get; init; }
}