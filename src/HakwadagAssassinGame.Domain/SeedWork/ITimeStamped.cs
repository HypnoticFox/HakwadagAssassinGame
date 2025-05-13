namespace HakwadagAssassinGame.Domain.SeedWork;

public interface ITimeStamped
{
    public uint ConcurrencyToken { get; }
}