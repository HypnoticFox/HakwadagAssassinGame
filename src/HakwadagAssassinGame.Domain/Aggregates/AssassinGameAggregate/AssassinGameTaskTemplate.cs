namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class AssassinGameTaskTemplate : Entity
{
    public required int Cost { get; init; }
    public required int Reward { get; init; }
    public required string Description { get; init; }
}