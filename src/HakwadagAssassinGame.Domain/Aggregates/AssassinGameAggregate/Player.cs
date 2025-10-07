namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class Player : ITimeStamped
{
    public Player(string applicationUserId)
    {
        ApplicationUserId = applicationUserId;
    }
    
    public Guid Id { get; }

    public Game? Game { get; }
    public Assignment? Assignment { get; private set; }

    public int GameId { get; }
    public string ApplicationUserId { get; }
    public int Score { get; private set; }

    public void SetAssignment(Assignment assignment)
    {
        if (Assignment is not null) throw new AssassinGameDomainException("Assignment is already set.");
        Assignment = assignment;
    }

    public void RemoveAssignment()
    {
        Assignment = null;
    }

    public void AddPoints(int points)
    {
        Score += points;
    }

    public uint ConcurrencyToken { get; }
}