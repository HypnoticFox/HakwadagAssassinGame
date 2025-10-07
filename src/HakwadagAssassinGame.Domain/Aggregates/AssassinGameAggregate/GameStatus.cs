namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class GameStatus : EnumerationWithCode
{
    public static readonly GameStatus Lobby = new(1, nameof(Lobby).ToLowerInvariant(), "Lobby");
    public static readonly GameStatus InProgress = new(2, nameof(InProgress).ToLowerInvariant(), "In Progress");
    public static readonly GameStatus Finished = new(3, nameof(Finished).ToLowerInvariant(), "Finished");

    public GameStatus(int id, string code, string name) : base(id, code, name)
    {
    }

    public static IEnumerable<GameStatus> ToList()
    {
        return GetAll<GameStatus>();
    }

    public static GameStatus FromId(int id)
    {
        var state = ToList().SingleOrDefault(s => s.Id == id);

        if (state == null)
            throw new AssassinGameDomainException(
                $"'{id}' is not a valid AssassinGameStatus. Possible values: {string.Join(",", ToList().Select(s => s.Id))}");

        return state;
    }

    public static GameStatus FromCode(string code)
    {
        var state = ToList()
            .SingleOrDefault(s => string.Equals(s.Code, code, StringComparison.InvariantCultureIgnoreCase));

        if (state == null)
            throw new AssassinGameDomainException(
                $"'{code}' is not a valid AssassinGameStatus. Possible values: {string.Join(",", ToList().Select(s => s.Name))}");

        return state;
    }
}