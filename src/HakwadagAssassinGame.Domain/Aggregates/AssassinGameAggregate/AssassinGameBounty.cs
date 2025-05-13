namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class AssassinGameBounty : TimeStampedEntity
{
    public int GameId { get; }
    public int TargetId { get; }
    public required int Reward { get; init; }
    public required string Description { get; init; }
}