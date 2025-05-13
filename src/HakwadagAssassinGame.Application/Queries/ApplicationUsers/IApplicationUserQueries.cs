namespace HakwadagAssassinGame.Application.Queries.ApplicationUsers;

public interface IApplicationUserQueries
{
    Task<Result<string>> GetUserDisplayNameAsync(string id, CancellationToken cancellationToken = default);
}