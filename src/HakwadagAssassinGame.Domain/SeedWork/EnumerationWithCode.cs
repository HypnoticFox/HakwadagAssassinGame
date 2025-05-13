namespace HakwadagAssassinGame.Domain.SeedWork;

public abstract class EnumerationWithCode : Enumeration
{
    protected EnumerationWithCode(int id, string code, string name)
        : base(id, name)
    {
        Code = code.ThrowIfNullOrEmpty();
    }

    public string Code { get; }

    public static T FromCode<T>(string code) where T : EnumerationWithCode
    {
        var matchingItem = Parse<T, string>(code, "code", item => item.Code == code);
        return matchingItem;
    }
}