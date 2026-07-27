using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Infrastructure.Persistence.Json;
using StackExchange.Redis;
using Condition = HakwadagAssassinGame.Core.Entities.Conditions.Condition;

namespace HakwadagAssassinGame.Infrastructure.Persistence.Redis;

/// <summary>Stores condition templates for each game in Redis.</summary>
public sealed class RedisConditionLibrary : RedisRepositoryBase, IConditionLibrary
{
    private const string KeyPrefix = "conditionlibrary";

    /// <summary>Initializes a Redis condition library.</summary>
    public RedisConditionLibrary(IConnectionMultiplexer connectionMultiplexer)
        : base(connectionMultiplexer)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Condition>> GetAsync(
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(Key(KeyPrefix, gameId), cancellationToken);
        var conditions = RedisJsonSerializer.Deserialize(value.ToString(), GameJsonContext.Default.ListCondition);
        if (conditions is not null)
        {
            return conditions;
        }

        conditions = CreateDefaults();
        await PersistAsync(gameId, conditions, cancellationToken);
        return conditions;
    }

    /// <inheritdoc />
    public async Task AddAsync(
        Guid gameId,
        Condition condition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(condition);
        var conditions = (await GetAsync(gameId, cancellationToken)).ToList();
        conditions.Add(condition);
        await PersistAsync(gameId, conditions, cancellationToken);
    }

    private Task PersistAsync(
        Guid gameId,
        List<Condition> conditions,
        CancellationToken cancellationToken) =>
        SetValueAsync(
            Key(KeyPrefix, gameId),
            RedisJsonSerializer.Serialize(conditions, GameJsonContext.Default.ListCondition),
            cancellationToken);

    private static List<Condition> CreateDefaults() =>
    [
        WithSpecificPersonCondition.Create(null),
        AloneCondition.Create(),
        WithXPeopleCondition.Create(2),
        MundaneActionCondition.Create("walking")
    ];
}
