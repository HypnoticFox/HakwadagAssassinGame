namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class Bounty : TimeStampedEntity
{
    public int GameId { get; }
    public Guid TargetId { get; }
    public required int Reward { get; init; }
    public required string Description { get; init; }
}