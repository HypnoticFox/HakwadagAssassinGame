namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class Game : TimeStampedEntity, IAggregateRoot
{
    private readonly List<Player> _players = [];
    
    private Game() { }

    public Game(string hostApplicationUserid)
    {
        HostApplicationUserId = hostApplicationUserid;
    }

    public int StatusId { get; private set; } = GameStatus.Lobby.Id;
    public GameStatus Status => GameStatus.FromId(StatusId);
    public string HostApplicationUserId { get; private set; }
    public IReadOnlyList<Player> Players => _players;
    public Bounty? Bounty { get; private set; }
    public bool AreHighScoresRevealed { get; private set; }

    public bool Archived { get; private set; }

    public Player AddPlayer(string applicationUserId)
    {
        var newPlayer = new Player(applicationUserId);
        _players.Add(newPlayer);
        return newPlayer;
    }

    public void RemovePlayer(Guid playerId)
    {
        var playerToRemove = _players.Single(p => p.Id == playerId);
        _players.Remove(playerToRemove);
    }

    public Bounty SetBounty(Bounty bounty)
    {
        if (Bounty is not null) throw new AssassinGameDomainException("Bounty is already set.");
        Bounty = bounty;
        return bounty;
    }
    
    public void RemoveBounty()
    {
        Bounty = null;
    }

    public void RevealHighScores()
    {
        AreHighScoresRevealed = true;
    }

    public void StartGame()
    {
        if (_players.Count < 3) throw new AssassinGameDomainException("Not enough players to start the game.");
        StatusId = GameStatus.InProgress.Id;
    }

    public void EndGame()
    {
        StatusId = GameStatus.Finished.Id;
        Archived = true;
    }
}