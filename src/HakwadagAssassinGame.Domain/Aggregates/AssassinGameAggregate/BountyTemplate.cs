namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class BountyTemplate : Entity
{
    public required string Description { get; init; }
}