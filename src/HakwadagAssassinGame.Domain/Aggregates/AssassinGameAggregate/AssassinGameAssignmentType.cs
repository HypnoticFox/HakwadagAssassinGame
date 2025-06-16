namespace HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

public sealed class AssassinGameAssignmentType : EnumerationWithCode
{
    public static readonly AssassinGameAssignmentType Default = new(1, nameof(Default).ToLowerInvariant(), "Default");
    public static readonly AssassinGameAssignmentType Betting = new(2, nameof(Betting).ToLowerInvariant(), "Betting");

    public AssassinGameAssignmentType(int id, string code, string name) : base(id, code, name)
    {
    }

    public static IEnumerable<AssassinGameAssignmentType> ToList()
    {
        return GetAll<AssassinGameAssignmentType>();
    }

    public static AssassinGameAssignmentType FromId(int id)
    {
        var state = ToList().SingleOrDefault(s => s.Id == id);

        if (state == null)
            throw new AssassinGameDomainException(
                $"'{id}' is not a valid AssassinGameAssignmentType. Possible values: {string.Join(",", ToList().Select(s => s.Id))}");

        return state;
    }

    public static AssassinGameAssignmentType FromCode(string code)
    {
        var state = ToList()
            .SingleOrDefault(s => string.Equals(s.Code, code, StringComparison.InvariantCultureIgnoreCase));

        if (state == null)
            throw new AssassinGameDomainException(
                $"'{code}' is not a valid AssassinGameAssignmentType. Possible values: {string.Join(",", ToList().Select(s => s.Name))}");

        return state;
    }
}