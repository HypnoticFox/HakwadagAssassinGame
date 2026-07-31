using System.Collections.Concurrent;
using HakwadagAssassinGame.Application.Interfaces;
using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Entities.Conditions;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;
using HakwadagAssassinGame.Infrastructure.Persistence.Json;

namespace HakwadagAssassinGame.Tests.Integration.Api;

/// <summary>
/// In-memory implementations of repository interfaces for API integration testing.
/// Uses JSON round-trip serialization for proper cloning that preserves all properties
/// including those with private setters.
/// </summary>

public sealed class InMemoryGameRepository : IGameRepository
{
    private readonly ConcurrentDictionary<Guid, string> _games = new();
    private readonly ConcurrentDictionary<string, Guid> _inviteCodes = new(StringComparer.OrdinalIgnoreCase);

    private static string Serialize(Game game) =>
        System.Text.Json.JsonSerializer.Serialize(game, GameJsonContext.Default.Game);

    private static Game? Deserialize(string json) =>
        System.Text.Json.JsonSerializer.Deserialize(json, GameJsonContext.Default.Game);

    public Task<Game?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (_games.TryGetValue(id, out var json))
            return Task.FromResult(Deserialize(json));
        return Task.FromResult<Game?>(null);
    }

    public Task<Game?> GetByInviteCodeAsync(string inviteCode, CancellationToken ct = default)
    {
        if (_inviteCodes.TryGetValue(inviteCode, out var id) && _games.TryGetValue(id, out var json))
            return Task.FromResult(Deserialize(json));
        return Task.FromResult<Game?>(null);
    }

    public Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Game>>(
            _games.Values.Select(json => Deserialize(json)!).ToList());

    public Task AddAsync(Game game, CancellationToken ct = default)
    {
        _games[game.Id] = Serialize(game);
        _inviteCodes[game.InviteCode] = game.Id;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Game game, CancellationToken ct = default)
    {
        if (_games.TryGetValue(game.Id, out var existingJson))
        {
            var existing = Deserialize(existingJson);
            if (existing is not null && !string.Equals(existing.InviteCode, game.InviteCode, StringComparison.Ordinal))
            {
                _inviteCodes.TryRemove(existing.InviteCode, out _);
            }
        }
        _games[game.Id] = Serialize(game);
        _inviteCodes[game.InviteCode] = game.Id;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (_games.TryRemove(id, out var json))
        {
            var game = Deserialize(json);
            if (game is not null)
                _inviteCodes.TryRemove(game.InviteCode, out _);
        }
        return Task.CompletedTask;
    }
}

public sealed class InMemoryPlayerRepository : IPlayerRepository
{
    private readonly ConcurrentDictionary<Guid, string> _players = new();
    private readonly ConcurrentDictionary<string, Guid> _emails = new(StringComparer.OrdinalIgnoreCase);

    private static string Serialize(Player p) =>
        System.Text.Json.JsonSerializer.Serialize(p, GameJsonContext.Default.Player);

    private static Player? Deserialize(string json) =>
        System.Text.Json.JsonSerializer.Deserialize(json, GameJsonContext.Default.Player);

    public Task<Player?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (_players.TryGetValue(id, out var json))
            return Task.FromResult(Deserialize(json));
        return Task.FromResult<Player?>(null);
    }

    public Task<Player?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        if (_emails.TryGetValue(email, out var id) && _players.TryGetValue(id, out var json))
            return Task.FromResult(Deserialize(json));
        return Task.FromResult<Player?>(null);
    }

    public Task AddAsync(Player player, CancellationToken ct = default)
    {
        _players[player.Id] = Serialize(player);
        _emails[player.Email] = player.Id;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Player player, CancellationToken ct = default)
    {
        if (_players.TryGetValue(player.Id, out var existingJson))
        {
            var existing = Deserialize(existingJson);
            if (existing is not null && !string.Equals(existing.Email, player.Email, StringComparison.Ordinal))
            {
                _emails.TryRemove(existing.Email, out _);
            }
        }
        _players[player.Id] = Serialize(player);
        _emails[player.Email] = player.Id;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (_players.TryRemove(id, out var json))
        {
            var player = Deserialize(json);
            if (player is not null)
                _emails.TryRemove(player.Email, out _);
        }
        return Task.CompletedTask;
    }
}

public sealed class InMemoryGamePlayerRepository : IGamePlayerRepository
{
    private readonly ConcurrentDictionary<string, string> _data = new();

    private static string Key(Guid gameId, Guid playerId) => $"gp:{gameId}:{playerId}";

    private static string Serialize(GamePlayer gp) =>
        System.Text.Json.JsonSerializer.Serialize(gp, GameJsonContext.Default.GamePlayer);

    private static GamePlayer? Deserialize(string json) =>
        System.Text.Json.JsonSerializer.Deserialize(json, GameJsonContext.Default.GamePlayer);

    public Task<GamePlayer?> GetAsync(Guid gameId, Guid playerId, CancellationToken ct = default)
    {
        if (_data.TryGetValue(Key(gameId, playerId), out var json))
            return Task.FromResult(Deserialize(json));
        return Task.FromResult<GamePlayer?>(null);
    }

    public Task<IReadOnlyList<GamePlayer>> GetByGameIdAsync(Guid gameId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<GamePlayer>>(
            _data.Values
                .Select(json => Deserialize(json)!)
                .Where(gp => gp!.GameId == gameId)
                .ToList()!);

    public Task<IReadOnlyList<GamePlayer>> GetByPlayerIdAsync(Guid playerId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<GamePlayer>>(
            _data.Values
                .Select(json => Deserialize(json)!)
                .Where(gp => gp!.PlayerId == playerId)
                .ToList()!);

    public Task AddAsync(GamePlayer gp, CancellationToken ct = default)
    {
        _data[Key(gp.GameId, gp.PlayerId)] = Serialize(gp);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(GamePlayer gp, CancellationToken ct = default)
    {
        _data[Key(gp.GameId, gp.PlayerId)] = Serialize(gp);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid gameId, Guid playerId, CancellationToken ct = default)
    {
        _data.TryRemove(Key(gameId, playerId), out _);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryAssignmentRepository : IAssignmentRepository
{
    private readonly ConcurrentDictionary<Guid, string> _data = new();

    private static string Serialize(Assignment a) =>
        System.Text.Json.JsonSerializer.Serialize(a, GameJsonContext.Default.Assignment);

    private static Assignment? Deserialize(string json) =>
        System.Text.Json.JsonSerializer.Deserialize(json, GameJsonContext.Default.Assignment);

    public Task<Assignment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (_data.TryGetValue(id, out var json))
            return Task.FromResult(Deserialize(json));
        return Task.FromResult<Assignment?>(null);
    }

    public Task<Assignment?> GetActiveByHunterIdAsync(Guid gameId, Guid hunterId, CancellationToken ct = default)
    {
        var assignment = _data.Values
            .Select(json => Deserialize(json)!)
            .FirstOrDefault(a => a!.GameId == gameId && a.HunterId == hunterId && a.Status == AssignmentStatus.Active);
        return Task.FromResult(assignment);
    }

    public Task<IReadOnlyList<Assignment>> GetByGameIdAsync(Guid gameId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Assignment>>(
            _data.Values
                .Select(json => Deserialize(json)!)
                .Where(a => a!.GameId == gameId)
                .ToList()!);

    public Task AddAsync(Assignment assignment, CancellationToken ct = default)
    {
        _data[assignment.Id] = Serialize(assignment);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Assignment assignment, CancellationToken ct = default)
    {
        _data[assignment.Id] = Serialize(assignment);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryTagSubmissionRepository : ITagSubmissionRepository
{
    private readonly ConcurrentDictionary<Guid, string> _data = new();

    private static string Serialize(TagSubmission s) =>
        System.Text.Json.JsonSerializer.Serialize(s, GameJsonContext.Default.TagSubmission);

    private static TagSubmission? Deserialize(string json) =>
        System.Text.Json.JsonSerializer.Deserialize(json, GameJsonContext.Default.TagSubmission);

    public Task<TagSubmission?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (_data.TryGetValue(id, out var json))
            return Task.FromResult(Deserialize(json));
        return Task.FromResult<TagSubmission?>(null);
    }

    public Task<IReadOnlyList<TagSubmission>> GetPendingByTargetIdAsync(Guid targetId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TagSubmission>>(
            _data.Values
                .Select(json => Deserialize(json)!)
                .Where(s => s!.TargetId == targetId && s.Status == TagStatus.Pending)
                .ToList()!);

    public Task AddAsync(TagSubmission submission, CancellationToken ct = default)
    {
        _data[submission.Id] = Serialize(submission);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TagSubmission submission, CancellationToken ct = default)
    {
        _data[submission.Id] = Serialize(submission);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryConditionLibrary : IConditionLibrary
{
    private readonly ConcurrentDictionary<Guid, List<Condition>> _libraries = new();

    public Task<IReadOnlyList<Condition>> GetAsync(Guid gameId, CancellationToken ct = default)
    {
        if (!_libraries.TryGetValue(gameId, out var conditions))
        {
            conditions =
            [
                WithSpecificPersonCondition.Create(null),
                AloneCondition.Create(),
                WithXPeopleCondition.Create(2),
                MundaneActionCondition.Create("walking")
            ];
            _libraries[gameId] = conditions;
        }
        return Task.FromResult<IReadOnlyList<Condition>>(conditions.ToList());
    }

    public Task AddAsync(Guid gameId, Condition condition, CancellationToken ct = default)
    {
        var list = _libraries.GetOrAdd(gameId, _ => []);
        list.Add(condition);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryOtpService : IOtpService
{
    private readonly ConcurrentDictionary<string, string> _otps = new(StringComparer.OrdinalIgnoreCase);

    public Task SendOtpAsync(string email, CancellationToken ct = default)
    {
        _otps[email.ToLowerInvariant()] = "123456";
        return Task.CompletedTask;
    }

    public Task<bool> VerifyOtpAsync(string email, string otp, CancellationToken ct = default)
    {
        var key = email.ToLowerInvariant();
        if (_otps.TryGetValue(key, out var stored) && stored == otp)
        {
            _otps.TryRemove(key, out _);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public void SetOtp(string email, string otp)
        => _otps[email.ToLowerInvariant()] = otp;
}

public sealed class InMemoryTokenStore : ITokenStore
{
    private readonly ConcurrentDictionary<string, Guid> _tokens = new(StringComparer.Ordinal);

    public Task StoreAsync(string token, Guid playerId, CancellationToken ct = default)
    {
        _tokens[token] = playerId;
        return Task.CompletedTask;
    }

    public Task<Guid?> GetPlayerIdAsync(string token, CancellationToken ct = default)
        => Task.FromResult(_tokens.TryGetValue(token, out var id) ? id : (Guid?)null);

    public Task RemoveAsync(string token, CancellationToken ct = default)
    {
        _tokens.TryRemove(token, out _);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryDevCounter : IDevCounter
{
    private readonly ConcurrentDictionary<string, long> _counters = new(StringComparer.Ordinal);

    public Task<long> IncrementAsync(string name, CancellationToken ct = default)
        => Task.FromResult(_counters.AddOrUpdate(name, 1, (_, v) => v + 1));
}
