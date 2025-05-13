using HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;

namespace HakwadagAssassinGame.Domain.Aggregates;

public interface IApplicationQueryables
{
    public IQueryable<ApplicationUser> Users { get; }
    // public IQueryable<AssassinGame> Games { get; }
}