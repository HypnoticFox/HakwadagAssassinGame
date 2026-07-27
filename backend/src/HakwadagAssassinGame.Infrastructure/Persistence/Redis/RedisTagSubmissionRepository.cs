using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;
using HakwadagAssassinGame.Infrastructure.Persistence.Json;
using StackExchange.Redis;

namespace HakwadagAssassinGame.Infrastructure.Persistence.Redis;

/// <summary>Stores tag submissions and pending-target indexes in Redis.</summary>
public sealed class RedisTagSubmissionRepository : RedisRepositoryBase, ITagSubmissionRepository
{
    /// <summary>Initializes a Redis tag submission repository.</summary>
    public RedisTagSubmissionRepository(IConnectionMultiplexer connectionMultiplexer)
        : base(connectionMultiplexer)
    {
    }

    /// <inheritdoc />
    public async Task<TagSubmission?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(Key("tag", id), cancellationToken);
        return RedisJsonSerializer.Deserialize(value.ToString(), GameJsonContext.Default.TagSubmission);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagSubmission>> GetPendingByTargetIdAsync(
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        var ids = await GetIdsAsync($"tag:pending:target:{targetId}", cancellationToken);
        var submissions = new List<TagSubmission>(ids.Count);
        foreach (var id in ids)
        {
            var submission = await GetByIdAsync(id, cancellationToken);
            if (submission is { Status: TagStatus.Pending })
            {
                submissions.Add(submission);
            }
        }

        return submissions;
    }

    /// <inheritdoc />
    public async Task AddAsync(
        TagSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        await SetValueAsync(
            Key("tag", submission.Id),
            RedisJsonSerializer.Serialize(submission, GameJsonContext.Default.TagSubmission),
            cancellationToken);
        await UpdatePendingIndexAsync(submission, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        TagSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        var existing = await GetByIdAsync(submission.Id, cancellationToken);
        if (existing is not null && existing.Status == TagStatus.Pending)
        {
            await RemoveFromSetAsync(
                $"tag:pending:target:{existing.TargetId}",
                existing.Id.ToString(),
                cancellationToken);
        }

        await SetValueAsync(
            Key("tag", submission.Id),
            RedisJsonSerializer.Serialize(submission, GameJsonContext.Default.TagSubmission),
            cancellationToken);
        await UpdatePendingIndexAsync(submission, cancellationToken);
    }

    private Task UpdatePendingIndexAsync(
        TagSubmission submission,
        CancellationToken cancellationToken) =>
        submission.Status == TagStatus.Pending
            ? AddToSetAsync(
                $"tag:pending:target:{submission.TargetId}",
                submission.Id.ToString(),
                cancellationToken)
            : Task.CompletedTask;
}
