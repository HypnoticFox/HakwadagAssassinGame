namespace HakwadagAssassinGame.Application.Interfaces;

/// <summary>Stores temporary authentication tokens.</summary>
public interface ITokenStore
{
    /// <summary>Stores a token for a player.</summary>
    Task StoreAsync(string token, Guid playerId, CancellationToken cancellationToken = default);

    /// <summary>Gets the player associated with a token.</summary>
    Task<Guid?> GetPlayerIdAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Removes a token.</summary>
    Task RemoveAsync(string token, CancellationToken cancellationToken = default);
}
