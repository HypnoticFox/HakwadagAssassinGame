namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class AssassinGamePlayer : TimeStampedEntity
{
    public AssassinGamePlayer(string applicationUserId)
    {
        ApplicationUserId = applicationUserId;
    }

    public AssassinGame Game { get; }
    public AssassinGameAssignment? Assignment { get; private set; }

    public int GameId { get; }
    public string ApplicationUserId { get; }
    public int Score { get; private set; }

    public void SetAssignment(AssassinGameAssignment assignment)
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
}