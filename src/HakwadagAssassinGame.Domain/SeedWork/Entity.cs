namespace HakwadagAssassinGame.Domain.SeedWork;

public abstract class Entity
{
    private int? _requestedHashCode;
    public virtual int Id { get; }

    public bool IsTransient()
    {
        return Id == 0;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || !(obj is Entity))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (GetType() != obj.GetType())
            return false;

        var item = (Entity)obj;

        if (item.IsTransient() || IsTransient())
            return false;
        return item.Id == Id;
    }

    protected bool Equals(Entity other)
    {
        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        if (IsTransient()) return base.GetHashCode();

        _requestedHashCode ??= Id.GetHashCode() ^ 31;
        // XOR for random distribution (http://blogs.msdn.com/b/ericlippert/archive/2011/02/28/guidelines-and-rules-for-gethashcode.aspx)

        return _requestedHashCode.Value;
    }

    public static bool operator ==(Entity left, Entity right)
    {
        return left?.Equals(right) ?? Equals(right, null);
    }

    public static bool operator !=(Entity left, Entity right)
    {
        return !(left == right);
    }
}