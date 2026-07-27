using HakwadagAssassinGame.Core.Entities;

namespace HakwadagAssassinGame.Core.Interfaces;

/// <summary>Provides persistence operations for tag submissions.</summary>
public interface ITagSubmissionRepository
{
    /// <summary>Gets a submission by identifier.</summary>
    Task<TagSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets pending submissions for a target.</summary>
    Task<IReadOnlyList<TagSubmission>> GetPendingByTargetIdAsync(Guid targetId, CancellationToken cancellationToken = default);

    /// <summary>Adds a tag submission.</summary>
    Task AddAsync(TagSubmission submission, CancellationToken cancellationToken = default);

    /// <summary>Updates a tag submission.</summary>
    Task UpdateAsync(TagSubmission submission, CancellationToken cancellationToken = default);
}
