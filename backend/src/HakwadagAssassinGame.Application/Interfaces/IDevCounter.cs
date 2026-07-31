namespace HakwadagAssassinGame.Application.Interfaces;

/// <summary>Provides atomic counter operations for development scenarios.</summary>
public interface IDevCounter
{
    /// <summary>Increments a named counter and returns the new value.</summary>
    Task<long> IncrementAsync(string name, CancellationToken cancellationToken = default);
}
