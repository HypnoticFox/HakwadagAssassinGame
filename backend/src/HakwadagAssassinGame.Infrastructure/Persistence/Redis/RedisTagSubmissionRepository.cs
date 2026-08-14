using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;
using HakwadagAssassinGame.Infrastructure.Persistence.Json;
using StackExchange.Redis;

namespace HakwadagAssassinGame.Infrastructure.Persistence.Redis;

/// <summary>Stores tag submissions and pending-target and pending-hunter indexes in Redis.</summary>
public sealed class RedisTagSubmissionRepository : RedisRepositoryBase, ITagSubmissionRepository
{
    private const string AllTagsKey = "tags:all";

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
    public async Task<IReadOnlyList<TagSubmission>> GetPendingByHunterIdAsync(
        Guid hunterId,
        CancellationToken cancellationToken = default)
    {
        var ids = await GetIdsAsync($"tag:pending:hunter:{hunterId}", cancellationToken);
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
    public async Task<IReadOnlyList<TagSubmission>> GetAllPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var ids = await GetIdsAsync(AllTagsKey, cancellationToken);
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
    public async Task<IReadOnlyList<TagSubmission>> GetByAssignmentIdAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var ids = await GetIdsAsync($"tag:assignment:{assignmentId}", cancellationToken);
        var submissions = new List<TagSubmission>(ids.Count);
        foreach (var id in ids)
        {
            var submission = await GetByIdAsync(id, cancellationToken);
            if (submission is not null)
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
        await AddToSetAsync(AllTagsKey, submission.Id.ToString(), cancellationToken);
        await AddToSetAsync(
            $"tag:assignment:{submission.AssignmentId}",
            submission.Id.ToString(),
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
            await RemoveFromSetAsync(
                $"tag:pending:hunter:{existing.HunterId}",
                existing.Id.ToString(),
                cancellationToken);
        }

        if (existing is not null && existing.AssignmentId != submission.AssignmentId)
        {
            await RemoveFromSetAsync(
                $"tag:assignment:{existing.AssignmentId}",
                existing.Id.ToString(),
                cancellationToken);
        }

        await SetValueAsync(
            Key("tag", submission.Id),
            RedisJsonSerializer.Serialize(submission, GameJsonContext.Default.TagSubmission),
            cancellationToken);
        await AddToSetAsync(
            $"tag:assignment:{submission.AssignmentId}",
            submission.Id.ToString(),
            cancellationToken);
        await UpdatePendingIndexAsync(submission, cancellationToken);
    }

    private Task UpdatePendingIndexAsync(
        TagSubmission submission,
        CancellationToken cancellationToken)
    {
        if (submission.Status != TagStatus.Pending)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(
            AddToSetAsync(
                $"tag:pending:target:{submission.TargetId}",
                submission.Id.ToString(),
                cancellationToken),
            AddToSetAsync(
                $"tag:pending:hunter:{submission.HunterId}",
                submission.Id.ToString(),
                cancellationToken));
    }
}
