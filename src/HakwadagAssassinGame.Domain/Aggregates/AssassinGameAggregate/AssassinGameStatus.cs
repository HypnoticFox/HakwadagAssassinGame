namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class AssassinGameStatus : EnumerationWithCode
{
    public static readonly AssassinGameStatus Lobby = new(1, nameof(Lobby).ToLowerInvariant(), "Lobby");
    public static readonly AssassinGameStatus InProgress = new(2, nameof(InProgress).ToLowerInvariant(), "In Progress");
    public static readonly AssassinGameStatus Finished = new(3, nameof(Finished).ToLowerInvariant(), "Finished");

    public AssassinGameStatus(int id, string code, string name) : base(id, code, name)
    {
    }

    public static IEnumerable<AssassinGameStatus> List()
    {
        return GetAll<AssassinGameStatus>();
    }

    public static AssassinGameStatus FromId(int id)
    {
        var state = List().SingleOrDefault(s => s.Id == id);

        if (state == null)
            throw new AssassinGameDomainException(
                $"'{id}' is not a valid AssassinGameStatus. Possible values: {string.Join(",", List().Select(s => s.Id))}");

        return state;
    }

    public static AssassinGameStatus FromCode(string code)
    {
        var state = List()
            .SingleOrDefault(s => string.Equals(s.Code, code, StringComparison.InvariantCultureIgnoreCase));

        if (state == null)
            throw new AssassinGameDomainException(
                $"'{code}' is not a valid AssassinGameStatus. Possible values: {string.Join(",", List().Select(s => s.Name))}");

        return state;
    }
}