using HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;
using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

namespace HakwadagAssassinGame.Domain.Aggregates;

public interface IApplicationQueryables
{
    public IQueryable<ApplicationUser> Users { get; }
    public IQueryable<AssassinGame> Games { get; }
}