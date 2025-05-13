using HakwadagAssassinGame.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HakwadagAssassinGame.Application.Queries.ApplicationUsers;

public sealed class ApplicationUserQueries : IApplicationUserQueries
{
    private readonly IFusionCache _cache;
    private readonly IApplicationQueryables _db;
    private readonly ILogger<ApplicationUserQueries> _logger;

    public ApplicationUserQueries(IApplicationQueryables db, ILogger<ApplicationUserQueries> logger,
        IFusionCache cache)
    {
        _db = db;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<string>> GetUserDisplayNameAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"user:display_name:{id}";

            var displayName = await _cache.GetOrSetAsync<string>(cacheKey,
                async (ctx, ct) => await GetUserDisplayNameInternalAsync(ctx, id, ct),
                options => options.SetDuration(TimeSpan.FromHours(12)),
                cancellationToken
            );

            return Result.Success(displayName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            return Result.Error();
        }
    }

    private async Task<string> GetUserDisplayNameInternalAsync(
        FusionCacheFactoryExecutionContext<string> ctx,
        string id,
        CancellationToken ct)
    {
        var result = await _db.Users
            .Where(x => x.Id == id)
            .Select(x => x.DisplayName)
            .FirstOrDefaultAsync(ct);

        return result ?? ctx.Fail("User not found");
    }
}