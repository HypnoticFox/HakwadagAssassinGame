namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class AssassinGame : TimeStampedEntity, IAggregateRoot
{
    private readonly List<AssassinGamePlayer> _players = [];

    private AssassinGame()
    {
    }

    public AssassinGame(string hostApplicationUserid)
    {
        HostApplicationUserId = hostApplicationUserid;
        AddPlayer(hostApplicationUserid);
    }

    public AssassinGameStatus Status { get; private set; } = AssassinGameStatus.Lobby;
    public string HostApplicationUserId { get; private set; }
    public IReadOnlyList<AssassinGamePlayer> Players => _players;

    public AssassinGameBounty Bounty { get; private set; }

    public bool Archived { get; private set; } = false;

    public AssassinGamePlayer AddPlayer(string applicationUserId)
    {
        var newPlayer = new AssassinGamePlayer(applicationUserId);
        _players.Add(newPlayer);
        return newPlayer;
    }
}