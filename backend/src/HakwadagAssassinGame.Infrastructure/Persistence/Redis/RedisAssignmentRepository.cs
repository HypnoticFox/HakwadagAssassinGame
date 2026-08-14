using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;
using HakwadagAssassinGame.Infrastructure.Persistence.Json;
using StackExchange.Redis;

namespace HakwadagAssassinGame.Infrastructure.Persistence.Redis;

/// <summary>Stores assignments and assignment lookup indexes in Redis.</summary>
public sealed class RedisAssignmentRepository : RedisRepositoryBase, IAssignmentRepository
{
    /// <summary>Initializes a Redis assignment repository.</summary>
    public RedisAssignmentRepository(IConnectionMultiplexer connectionMultiplexer)
        : base(connectionMultiplexer)
    {
    }

    /// <inheritdoc />
    public async Task<Assignment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(Key("assignment", id), cancellationToken);
        return RedisJsonSerializer.Deserialize(value.ToString(), GameJsonContext.Default.Assignment);
    }

    /// <inheritdoc />
    public async Task<Assignment?> GetActiveByHunterIdAsync(
        Guid gameId,
        Guid hunterId,
        CancellationToken cancellationToken = default)
    {
        var idValue = await GetValueAsync($"assignment:hunter:{gameId}:{hunterId}", cancellationToken);
        if (!Guid.TryParse(idValue.ToString(), out var id))
        {
            return null;
        }

        var assignment = await GetByIdAsync(id, cancellationToken);
        return assignment is { Status: AssignmentStatus.Active }
            && assignment.GameId == gameId
            && assignment.HunterId == hunterId
            ? assignment
            : null;
    }

    /// <inheritdoc />
    public async Task<Assignment?> GetMostRecentByHunterIdAsync(
        Guid gameId,
        Guid hunterId,
        CancellationToken cancellationToken = default)
    {
        var ids = await GetIdsAsync($"assignment:game:{gameId}", cancellationToken);
        Assignment? mostRecent = null;
        foreach (var id in ids)
        {
            var assignment = await GetByIdAsync(id, cancellationToken);
            if (assignment is null || assignment.HunterId != hunterId)
            {
                continue;
            }

            if (mostRecent is null || assignment.AssignedAt > mostRecent.AssignedAt)
            {
                mostRecent = assignment;
            }
        }

        return mostRecent;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Assignment>> GetByGameIdAsync(
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var ids = await GetIdsAsync($"assignment:game:{gameId}", cancellationToken);
        var assignments = new List<Assignment>(ids.Count);
        foreach (var id in ids)
        {
            var assignment = await GetByIdAsync(id, cancellationToken);
            if (assignment is not null)
            {
                assignments.Add(assignment);
            }
        }

        return assignments;
    }

    /// <inheritdoc />
    public async Task AddAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        await SetValueAsync(
            Key("assignment", assignment.Id),
            RedisJsonSerializer.Serialize(assignment, GameJsonContext.Default.Assignment),
            cancellationToken);
        await AddToSetAsync(
            $"assignment:game:{assignment.GameId}",
            assignment.Id.ToString(),
            cancellationToken);
        await UpdateActiveIndexAsync(assignment, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        var existing = await GetByIdAsync(assignment.Id, cancellationToken);
        if (existing is not null)
        {
            await RemoveActiveIndexAsync(existing, cancellationToken);
            if (existing.GameId != assignment.GameId)
            {
                await RemoveFromSetAsync(
                    $"assignment:game:{existing.GameId}",
                    existing.Id.ToString(),
                    cancellationToken);
            }
        }

        await SetValueAsync(
            Key("assignment", assignment.Id),
            RedisJsonSerializer.Serialize(assignment, GameJsonContext.Default.Assignment),
            cancellationToken);
        await AddToSetAsync(
            $"assignment:game:{assignment.GameId}",
            assignment.Id.ToString(),
            cancellationToken);
        await UpdateActiveIndexAsync(assignment, cancellationToken);
    }

    private async Task UpdateActiveIndexAsync(
        Assignment assignment,
        CancellationToken cancellationToken)
    {
        if (assignment.Status == AssignmentStatus.Active)
        {
            await SetValueAsync(
                $"assignment:hunter:{assignment.GameId}:{assignment.HunterId}",
                assignment.Id.ToString(),
                cancellationToken);
        }
    }

    private Task RemoveActiveIndexAsync(
        Assignment assignment,
        CancellationToken cancellationToken) =>
        assignment.Status == AssignmentStatus.Active
            ? DeleteKeyAsync($"assignment:hunter:{assignment.GameId}:{assignment.HunterId}", cancellationToken)
            : Task.CompletedTask;
}
