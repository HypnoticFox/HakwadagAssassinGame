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
    public int Score { get; private set; } = 0;
}