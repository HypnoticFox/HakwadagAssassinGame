namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class AssignmentType : EnumerationWithCode
{
    public static readonly AssignmentType Default = new(1, nameof(Default).ToLowerInvariant(), "Default");
    public static readonly AssignmentType Betting = new(2, nameof(Betting).ToLowerInvariant(), "Betting");

    public AssignmentType(int id, string code, string name) : base(id, code, name)
    {
    }

    public static IEnumerable<AssignmentType> ToList()
    {
        return GetAll<AssignmentType>();
    }

    public static AssignmentType FromId(int id)
    {
        var state = ToList().SingleOrDefault(s => s.Id == id);

        if (state == null)
            throw new AssassinGameDomainException(
                $"'{id}' is not a valid AssassinGameAssignmentType. Possible values: {string.Join(",", ToList().Select(s => s.Id))}");

        return state;
    }

    public static AssignmentType FromCode(string code)
    {
        var state = ToList()
            .SingleOrDefault(s => string.Equals(s.Code, code, StringComparison.InvariantCultureIgnoreCase));

        if (state == null)
            throw new AssassinGameDomainException(
                $"'{code}' is not a valid AssassinGameAssignmentType. Possible values: {string.Join(",", ToList().Select(s => s.Name))}");

        return state;
    }
}