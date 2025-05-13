using System.Reflection;

namespace HakwadagAssassinGame.Domain.SeedWork;

public abstract class Enumeration : IComparable
{
    protected Enumeration(int id, string name)
    {
        Id = id.ThrowIf(number => number < 1, "Id must be larger than 0");
        Name = name.ThrowIfNullOrEmpty("Name cannot be empty.");
    }

    public string Name { get; }

    public int Id { get; }

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is Enumeration other) return Id.CompareTo(other.Id);

        throw new ArgumentException($"Argument is not of type {nameof(Enumeration)}", nameof(obj));
    }

    public override string ToString()
    {
        return Name;
    }

    public static IEnumerable<T> GetAll<T>() where T : Enumeration
    {
        return typeof(T).GetFields(BindingFlags.Public |
                                   BindingFlags.Static |
                                   BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration otherValue) return false;

        var typeMatches = GetType().Equals(obj.GetType());
        var valueMatches = Id.Equals(otherValue.Id);

        return typeMatches && valueMatches;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static int AbsoluteDifference(Enumeration firstValue, Enumeration secondValue)
    {
        var absoluteDifference = Math.Abs(firstValue.Id - secondValue.Id);
        return absoluteDifference;
    }

    public static T FromValue<T>(int value) where T : Enumeration
    {
        var matchingItem = Parse<T, int>(value, "value", item => item.Id == value);
        return matchingItem;
    }

    public static T FromDisplayName<T>(string displayName) where T : Enumeration
    {
        var matchingItem = Parse<T, string>(displayName, "display name", item => item.Name == displayName);
        return matchingItem;
    }

    protected static T Parse<T, K>(K value, string description, Func<T, bool> predicate) where T : Enumeration
    {
        var matchingItem = GetAll<T>().FirstOrDefault(predicate);

        if (matchingItem == null)
            throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(T)}");

        return matchingItem;
    }
}