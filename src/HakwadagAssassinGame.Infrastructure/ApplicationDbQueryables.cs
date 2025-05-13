using HakwadagAssassinGame.Domain.Aggregates;
using HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;
using Microsoft.EntityFrameworkCore;

namespace HakwadagAssassinGame.Infrastructure;

public sealed class ApplicationDbQueryables : IApplicationQueryables
{
    private readonly ApplicationDbContext _context;

    public ApplicationDbQueryables(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<ApplicationUser> Users => _context.Users.AsNoTracking();

    // public IQueryable<AssassinGame> Games => _context.Games.AsNoTracking();
}