namespace HakwadagAssassinGame.Domain.SeedWork;

public abstract class TimeStampedEntity : Entity, ITimeStamped
{
    public uint ConcurrencyToken { get; }
}