using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;
using Microsoft.EntityFrameworkCore;

namespace HakwadagAssassinGame.Infrastructure.Repositories;

public sealed class AssassinGameRepository : IAssassinGameRepository
{
    private readonly ApplicationDbContext _context;

    public AssassinGameRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public Game Add(Game game)
    {
        _context.Games.Add(game);
        return game;
    }

    public async Task<Game?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var assassinGame = await _context
            .Games
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        assassinGame ??= _context
            .Games
            .Local
            .FirstOrDefault(x => x.Id == id);

        return assassinGame;
    }
}