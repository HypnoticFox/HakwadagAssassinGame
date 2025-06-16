namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class AssassinGame : TimeStampedEntity, IAggregateRoot
{
    private readonly List<AssassinGamePlayer> _players = [];

    public AssassinGame(string hostApplicationUserid)
    {
        HostApplicationUserId = hostApplicationUserid;
    }

    public int StatusId { get; private set; } = AssassinGameStatus.Lobby.Id;
    public AssassinGameStatus Status => AssassinGameStatus.FromId(StatusId);
    public string HostApplicationUserId { get; private set; }
    public IReadOnlyList<AssassinGamePlayer> Players => _players;
    public AssassinGameBounty? Bounty { get; private set; }
    public bool AreHighScoresRevealed { get; private set; }

    public bool Archived { get; private set; }

    public AssassinGamePlayer AddPlayer(string applicationUserId)
    {
        var newPlayer = new AssassinGamePlayer(applicationUserId);
        _players.Add(newPlayer);
        return newPlayer;
    }

    public void RemovePlayer(int playerId)
    {
        var playerToRemove = _players.Single(p => p.Id == playerId);
        _players.Remove(playerToRemove);
    }

    public AssassinGameBounty SetBounty(AssassinGameBounty bounty)
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
        StatusId = AssassinGameStatus.InProgress.Id;
    }

    public void EndGame()
    {
        StatusId = AssassinGameStatus.Finished.Id;
        Archived = true;
    }
}