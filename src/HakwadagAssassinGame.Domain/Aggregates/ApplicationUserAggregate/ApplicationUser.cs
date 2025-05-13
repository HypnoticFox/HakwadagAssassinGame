namespace HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;

public sealed class ApplicationUser : IAggregateRoot, ITimeStamped
{
    private string _displayName = string.Empty;
    private string _id = string.Empty;

    public ApplicationUser(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public string Id
    {
        get => _id;
        private set
        {
            if (string.IsNullOrEmpty(value)) throw new ApplicationUserDomainException("Id cannot be null or empty.");

            _id = value;
        }
    }

    public string DisplayName
    {
        get => _displayName;
        private set
        {
            if (string.IsNullOrEmpty(value))
                throw new ApplicationUserDomainException("Username cannot be null or empty.");
            _displayName = value.Length switch
            {
                < 5 => throw new ApplicationUserDomainException("Username should be at least 5 characters long."),
                > 15 => throw new ApplicationUserDomainException("Username should be at most 15 characters long."),
                _ => value
            };
        }
    }

    public uint ConcurrencyToken { get; }

    public void SetDisplayName(string displayName)
    {
        DisplayName = displayName;
    }
}