using System.Runtime.CompilerServices;
using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;

[assembly: InternalsVisibleTo("HakwadagAssassinGame.Tests")]

namespace HakwadagAssassinGame.Application.Services;

internal static class ServiceHelpers
{
    private static readonly string[] MundaneActions = ["eating", "drinking", "sitting", "talking", "walking", "laughing"];

    public static async Task<Player> RequirePlayerAsync(
        IPlayerRepository repository,
        Guid playerId,
        CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(playerId, cancellationToken)
        ?? throw new PlayerNotFoundException(playerId);

    public static async Task<Game> RequireGameAsync(
        IGameRepository repository,
        Guid gameId,
        CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(gameId, cancellationToken)
        ?? throw new GameNotFoundException(gameId);

    public static async Task<GamePlayer> RequireMembershipAsync(
        IGamePlayerRepository repository,
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken) =>
        await repository.GetAsync(gameId, playerId, cancellationToken)
        ?? throw new UnauthorizedException("The player is not a member of this game.");

    public static bool IsAdmin(GamePlayer membership) =>
        membership.Role is GameRole.Creator or GameRole.CoAdmin;

    public static async Task<List<Condition>> CreateConditions(
        Guid gameId,
        Guid hunterId,
        Guid targetId,
        IReadOnlyList<Player> players,
        IConditionLibrary library,
        CancellationToken cancellationToken)
    {
        var templates = await library.GetAsync(gameId, cancellationToken);
        if (templates.Count == 0)
        {
            templates = [AloneCondition.Create()];
        }

        var count = Math.Min(templates.Count, Random.Shared.Next(2, 4));
        var selected = templates.OrderBy(_ => Random.Shared.Next()).Take(count);
        var otherPlayers = players.Where(player => player.Id != hunterId && player.Id != targetId).ToArray();
        return selected.Select(condition => CloneCondition(condition, otherPlayers)).ToList();
    }

    public static AssignmentDto MapAssignment(Assignment assignment, Player target, IReadOnlyList<Player> players) =>
        new(
            assignment.Id,
            new TargetDto(target.Id, target.DisplayName, target.AvatarUrl),
            assignment.Conditions.Select(condition => MapCondition(condition, players)).ToList(),
            assignment.AssignedAt);

    public static ConditionDto MapCondition(Condition condition, IReadOnlyList<Player> players) => condition switch
    {
        WithSpecificPersonCondition specific => new(
            specific.Id,
            specific.Type,
            specific.Describe(),
            specific.TargetPersonId is Guid id
                ? players.FirstOrDefault(player => player.Id == id)?.DisplayName
                : null,
            null,
            null),
        MundaneActionCondition action => new(action.Id, action.Type, action.Describe(), null, action.Action, null),
        WithXPeopleCondition people => new(people.Id, people.Type, people.Describe(), null, null, people.MinPeople),
        _ => new(condition.Id, condition.Type, condition.Describe(), null, null, null)
    };

    public static TagSubmissionDto MapTag(Core.Entities.TagSubmission submission) => new(
        submission.Id,
        submission.AssignmentId,
        submission.HunterId,
        submission.TargetId,
        submission.ConditionId,
        submission.Status,
        submission.SubmittedAt,
        submission.ResolvedAt);

    public static List<Guid> CreateDerangement(IReadOnlyList<GamePlayer> memberships)
    {
        var players = memberships.Where(membership => membership.IsActive && membership.IsParticipating).Select(membership => membership.PlayerId).ToList();
        if (players.Count < 3)
        {
            throw new InvalidGameStateException("At least three participating players are required.");
        }

        var targets = players.ToList();
        do
        {
            targets = targets.OrderBy(_ => Random.Shared.Next()).ToList();
        } while (targets.Select((target, index) => target == players[index]).Any(static fixedPoint => fixedPoint));

        return targets;
    }

    public static Condition CloneCondition(Condition condition, IReadOnlyList<Player> otherPlayers) => condition switch
    {
        WithSpecificPersonCondition => WithSpecificPersonCondition.Create(
            otherPlayers.Count == 0 ? null : otherPlayers[Random.Shared.Next(otherPlayers.Count)].Id),
        AloneCondition => AloneCondition.Create(),
        WithXPeopleCondition people => WithXPeopleCondition.Create(people.MinPeople),
        MundaneActionCondition => MundaneActionCondition.Create(MundaneActions[Random.Shared.Next(MundaneActions.Length)]),
        CustomCondition custom => CustomCondition.Create(custom.Description),
        _ => AloneCondition.Create()
    };
}
