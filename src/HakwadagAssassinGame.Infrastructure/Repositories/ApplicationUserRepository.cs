using HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;
using Microsoft.EntityFrameworkCore;

namespace HakwadagAssassinGame.Infrastructure.Repositories;

public sealed class ApplicationUserRepository : IApplicationUserRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationUserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public ApplicationUser Add(ApplicationUser applicationUser)
    {
        _context.Users.Add(applicationUser);
        return applicationUser;
    }

    public void Update(ApplicationUser applicationUser)
    {
        _context.Users.Update(applicationUser);
    }

    public async Task<ApplicationUser?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var applicationUser = await _context
            .Users
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        applicationUser ??= _context
            .Users
            .Local
            .FirstOrDefault(x => x.Id == id);

        return applicationUser;
    }
}