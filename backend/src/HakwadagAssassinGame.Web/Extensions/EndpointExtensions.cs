using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Core.Interfaces;
using HakwadagAssassinGame.Infrastructure.Realtime;
using HakwadagAssassinGame.Web.Middleware;

namespace HakwadagAssassinGame.Web.Extensions;

/// <summary>Marks minimal API endpoints that require an authenticated player.</summary>
public sealed class RequirePlayerMetadata
{
}

/// <summary>Extensions for declaring authenticated minimal API endpoints.</summary>
public static class EndpointExtensions
{
    /// <summary>Registers all HTTP and realtime endpoints for the application.</summary>
    public static void MapEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth");
        auth.MapPost("/send-otp", async (SendOtpRequest request, IAuthService service, CancellationToken cancellationToken) =>
        {
            await service.SendOtpAsync(request.Email, cancellationToken);
            return Results.Ok();
        });
        auth.MapPost("/verify-otp", async (VerifyOtpRequest request, IAuthService service, CancellationToken cancellationToken) =>
        {
            var response = await service.VerifyOtpAsync(request.Email, request.Code, cancellationToken);
            return Results.Ok(new { token = response.Token, player = response.Player });
        });
        auth.MapGet("/me", (HttpContext context) =>
            Results.Ok(context.GetAuthenticatedPlayer())).RequirePlayer();

        var games = app.MapGroup("/api/games").RequirePlayer();
        games.MapPost("", async (HttpContext context, CreateGameRequest request, IGameService service, CancellationToken cancellationToken) =>
        {
            var game = await service.CreateGameAsync(context.GetRequiredPlayerId(), request, cancellationToken);
            return Results.Created($"/api/games/{game.Id}", game);
        });
        games.MapGet("", async (HttpContext context, IGameService service, CancellationToken cancellationToken) =>
        {
            var games = await service.GetMyGamesAsync(context.GetRequiredPlayerId(), cancellationToken);
            return Results.Ok(games);
        });
        games.MapGet("/{gameId:guid}", async (HttpContext context, Guid gameId, IGameService service, CancellationToken cancellationToken) =>
        {
            var game = await service.GetGameAsync(context.GetRequiredPlayerId(), gameId, cancellationToken);
            return Results.Ok(game);
        });
        games.MapGet("/{gameId:guid}/players", async (HttpContext context, Guid gameId, IGameService service, CancellationToken cancellationToken) =>
        {
            var players = await service.GetPlayersAsync(gameId, cancellationToken);
            return Results.Ok(players);
        });
        games.MapPost("/join/{inviteCode}", async (HttpContext context, string inviteCode, JoinGameRequest request, IGameService service, CancellationToken cancellationToken) =>
        {
            var game = await service.JoinGameAsync(context.GetRequiredPlayerId(), inviteCode, request, cancellationToken);
            return Results.Ok(game);
        });
        games.MapPost("/{gameId:guid}/start", async (HttpContext context, Guid gameId, IGameService service, CancellationToken cancellationToken) =>
        {
            var game = await service.StartGameAsync(context.GetRequiredPlayerId(), gameId, cancellationToken);
            return Results.Ok(game);
        });
        games.MapPost("/{gameId:guid}/end", async (HttpContext context, Guid gameId, IGameService service, CancellationToken cancellationToken) =>
        {
            var game = await service.EndGameAsync(context.GetRequiredPlayerId(), gameId, cancellationToken);
            return Results.Ok(game);
        });
        games.MapPost("/{gameId:guid}/leave", async (HttpContext context, Guid gameId, IGameService service, CancellationToken cancellationToken) =>
        {
            await service.LeaveGameAsync(context.GetRequiredPlayerId(), gameId, cancellationToken);
            return Results.Ok();
        });
        games.MapPost("/{gameId:guid}/rejoin", async (HttpContext context, Guid gameId, IGameService service, CancellationToken cancellationToken) =>
        {
            var game = await service.RejoinGameAsync(context.GetRequiredPlayerId(), gameId, cancellationToken);
            return Results.Ok(game);
        });
        games.MapPut("/{gameId:guid}/participation", async (HttpContext context, Guid gameId, SetParticipationRequest request, IAdminService service, CancellationToken cancellationToken) =>
        {
            await service.SetParticipationAsync(context.GetRequiredPlayerId(), gameId, request.IsParticipating, cancellationToken);
            return Results.Ok();
        });

        var assignments = games.MapGroup("/{gameId:guid}/assignments");
        assignments.MapGet("/me", async (HttpContext context, Guid gameId, IAssignmentService service, CancellationToken cancellationToken) =>
        {
            var assignment = await service.GetMyAssignmentAsync(context.GetRequiredPlayerId(), gameId, cancellationToken);
            return Results.Ok(assignment);
        });

        var tag = games.MapGroup("/{gameId:guid}/tag");
        tag.MapPost("", async (HttpContext context, Guid gameId, SubmitTagRequest request, ITagService service, CancellationToken cancellationToken) =>
        {
            var tagSubmission = await service.SubmitTagAsync(context.GetRequiredPlayerId(), request, cancellationToken);
            return Results.Created($"/api/games/{gameId}/tag/{tagSubmission.Id}", tagSubmission);
        });
        tag.MapGet("/pending", async (HttpContext context, Guid gameId, ITagService service, CancellationToken cancellationToken) =>
        {
            var tagSubmission = await service.GetPendingTagAsync(context.GetRequiredPlayerId(), gameId, cancellationToken);
            return tagSubmission is null ? Results.NotFound() : Results.Ok(tagSubmission);
        });
        tag.MapPost("/{tagId:guid}/confirm", async (HttpContext context, Guid tagId, ITagService service, CancellationToken cancellationToken) =>
        {
            var tagSubmission = await service.ConfirmTagAsync(context.GetRequiredPlayerId(), tagId, cancellationToken);
            return Results.Ok(tagSubmission);
        });
        tag.MapPost("/{tagId:guid}/deny", async (HttpContext context, Guid tagId, ITagService service, CancellationToken cancellationToken) =>
        {
            var tagSubmission = await service.DenyTagAsync(context.GetRequiredPlayerId(), tagId, cancellationToken);
            return Results.Ok(tagSubmission);
        });
        tag.MapPost("/{tagId:guid}/void", async (HttpContext context, Guid tagId, ITagService service, CancellationToken cancellationToken) =>
        {
            var tagSubmission = await service.VoidTagAsync(context.GetRequiredPlayerId(), tagId, cancellationToken);
            return Results.Ok(tagSubmission);
        });

        var leaderboard = games.MapGroup("/{gameId:guid}/leaderboard");
        leaderboard.MapGet("", async (Guid gameId, ILeaderboardService service, CancellationToken cancellationToken) =>
        {
            var leaderboard = await service.GetLeaderboardAsync(gameId, cancellationToken);
            return Results.Ok(leaderboard);
        });

        var admins = games.MapGroup("/{gameId:guid}/admins");
        admins.MapPost("", async (HttpContext context, Guid gameId, AddAdminRequest request, IAdminService service, CancellationToken cancellationToken) =>
        {
            await service.AddCoAdminAsync(context.GetRequiredPlayerId(), gameId, request.PlayerId, cancellationToken);
            return Results.Ok();
        });
        admins.MapDelete("/{playerId:guid}", async (HttpContext context, Guid gameId, Guid playerId, IAdminService service, CancellationToken cancellationToken) =>
        {
            await service.RemoveCoAdminAsync(context.GetRequiredPlayerId(), gameId, playerId, cancellationToken);
            return Results.Ok();
        });

        var safeTimes = games.MapGroup("/{gameId:guid}/safe-times");
        safeTimes.MapPost("", async (HttpContext context, Guid gameId, AddSafeTimeBlockRequest request, IAdminService service, CancellationToken cancellationToken) =>
        {
            var blockId = await service.AddSafeTimeBlockAsync(context.GetRequiredPlayerId(), gameId, request, cancellationToken);
            return Results.Created($"/api/games/{gameId}/safe-times/{blockId}", new { blockId });
        });
        safeTimes.MapDelete("/{blockId:guid}", async (HttpContext context, Guid gameId, Guid blockId, IAdminService service, CancellationToken cancellationToken) =>
        {
            await service.RemoveSafeTimeBlockAsync(context.GetRequiredPlayerId(), gameId, blockId, cancellationToken);
            return Results.Ok();
        });

        var conditions = games.MapGroup("/{gameId:guid}/conditions");
        conditions.MapPost("", async (HttpContext context, Guid gameId, AddCustomConditionRequest request, IAdminService service, CancellationToken cancellationToken) =>
        {
            await service.AddCustomConditionAsync(context.GetRequiredPlayerId(), gameId, request, cancellationToken);
            return Results.Created($"/api/games/{gameId}/conditions", null);
        });

        // Dev-only testing endpoints — not available in production.
        if (app.Environment.IsDevelopment())
        {
            app.MapPost("/api/auth/dev-login", async (DevLoginRequest request, IDevSeedService service, CancellationToken cancellationToken) =>
            {
                var response = await service.DevLoginAsync(request, cancellationToken);
                return Results.Ok(new { token = response.Token, player = response.Player });
            });

            app.MapPost("/api/dev/seed-game", async (SeedGameRequest request, IDevSeedService service, CancellationToken cancellationToken) =>
            {
                var response = await service.SeedGameAsync(request, cancellationToken);
                return Results.Ok(response);
            });

            // ── Dev Dashboard — Game list ──────────────────────────────────────

            app.MapGet("/api/dev/games", async (
                IGameRepository gameRepo,
                IGamePlayerRepository gamePlayerRepo,
                CancellationToken ct) =>
            {
                var games = await gameRepo.GetAllAsync(ct);
                var result = new List<DevGameSummaryDto>(games.Count);
                foreach (var game in games)
                {
                    var memberships = await gamePlayerRepo.GetByGameIdAsync(game.Id, ct);
                    result.Add(new DevGameSummaryDto(
                        game.Id,
                        game.Name,
                        game.InviteCode,
                        game.Status,
                        memberships.Count(m => m.IsActive),
                        game.CreatedAt,
                        game.ScheduledEndAt));
                }

                return Results.Ok(result);
            });

            var devGames = app.MapGroup("/api/dev/games/{gameId:guid}");

            // ── Dev Dashboard — Players in a game ──────────────────────────────

            devGames.MapGet("/players", async (
                Guid gameId,
                IGameRepository gameRepo,
                IGamePlayerRepository gamePlayerRepo,
                IPlayerRepository playerRepo,
                CancellationToken ct) =>
            {
                var game = await gameRepo.GetByIdAsync(gameId, ct);
                if (game is null)
                {
                    return Results.NotFound(new { error = "Game not found." });
                }

                var memberships = await gamePlayerRepo.GetByGameIdAsync(gameId, ct);
                var result = new List<DevPlayerInGameDto>(memberships.Count);
                foreach (var membership in memberships)
                {
                    var player = await playerRepo.GetByIdAsync(membership.PlayerId, ct);
                    result.Add(new DevPlayerInGameDto(
                        membership.PlayerId,
                        player?.Email ?? "unknown",
                        player?.DisplayName ?? "unknown",
                        membership.Role,
                        membership.Score,
                        membership.IsActive,
                        membership.IsParticipating));
                }

                return Results.Ok(result);
            });

            // ── Dev Dashboard — Assignments in a game ──────────────────────────

            devGames.MapGet("/assignments", async (
                Guid gameId,
                IGameRepository gameRepo,
                IAssignmentRepository assignmentRepo,
                IPlayerRepository playerRepo,
                CancellationToken ct) =>
            {
                var game = await gameRepo.GetByIdAsync(gameId, ct);
                if (game is null)
                {
                    return Results.NotFound(new { error = "Game not found." });
                }

                var assignments = await assignmentRepo.GetByGameIdAsync(gameId, ct);
                var result = new List<DevAssignmentDto>(assignments.Count);
                foreach (var assignment in assignments)
                {
                    var hunter = await playerRepo.GetByIdAsync(assignment.HunterId, ct);
                    var target = await playerRepo.GetByIdAsync(assignment.TargetId, ct);
                    result.Add(new DevAssignmentDto(
                        assignment.Id,
                        assignment.HunterId,
                        hunter?.DisplayName ?? "unknown",
                        assignment.TargetId,
                        target?.DisplayName ?? "unknown",
                        assignment.Status,
                        assignment.AssignedAt));
                }

                return Results.Ok(result);
            });

            // ── Dev Dashboard — Tag submissions in a game ──────────────────────

            devGames.MapGet("/tags", async (
                Guid gameId,
                IGameRepository gameRepo,
                IAssignmentRepository assignmentRepo,
                ITagSubmissionRepository tagSubmissionRepo,
                IPlayerRepository playerRepo,
                CancellationToken ct) =>
            {
                var game = await gameRepo.GetByIdAsync(gameId, ct);
                if (game is null)
                {
                    return Results.NotFound(new { error = "Game not found." });
                }

                var assignments = await assignmentRepo.GetByGameIdAsync(gameId, ct);
                var result = new List<DevTagSubmissionDto>();
                foreach (var assignment in assignments)
                {
                    var tags = await tagSubmissionRepo.GetByAssignmentIdAsync(assignment.Id, ct);
                    foreach (var tag in tags)
                    {
                        var hunter = await playerRepo.GetByIdAsync(tag.HunterId, ct);
                        var target = await playerRepo.GetByIdAsync(tag.TargetId, ct);
                        result.Add(new DevTagSubmissionDto(
                            tag.Id,
                            tag.AssignmentId,
                            tag.HunterId,
                            hunter?.DisplayName ?? "unknown",
                            tag.TargetId,
                            target?.DisplayName ?? "unknown",
                            tag.Status,
                            tag.SubmittedAt,
                            tag.ResolvedAt));
                    }
                }

                return Results.Ok(result);
            });

            // ── Dev Quick-Action — Submit tag on behalf of a player ───────────

            devGames.MapPost("/submit-tag", async (
                Guid gameId,
                DevSubmitTagRequest request,
                IGameRepository gameRepo,
                IPlayerRepository playerRepo,
                ITagService tagService,
                CancellationToken ct) =>
            {
                var game = await gameRepo.GetByIdAsync(gameId, ct);
                if (game is null)
                {
                    return Results.NotFound(new { error = "Game not found." });
                }

                var player = await playerRepo.GetByIdAsync(request.PlayerId, ct);
                if (player is null)
                {
                    return Results.NotFound(new { error = "Player not found." });
                }

                var result = await tagService.SubmitTagAsync(
                    request.PlayerId,
                    new SubmitTagRequest(request.AssignmentId, request.ConditionId),
                    ct);
                return Results.Ok(result);
            });

            // ── Dev Quick-Action — Confirm a tag ──────────────────────────────

            app.MapPost("/api/dev/tags/{tagId:guid}/confirm", async (
                Guid tagId,
                ITagSubmissionRepository tagSubmissionRepo,
                ITagService tagService,
                CancellationToken ct) =>
            {
                var submission = await tagSubmissionRepo.GetByIdAsync(tagId, ct);
                if (submission is null)
                {
                    return Results.NotFound(new { error = "Tag submission not found." });
                }

                var result = await tagService.ConfirmTagAsync(submission.TargetId, tagId, ct);
                return Results.Ok(result);
            });

            // ── Dev Quick-Action — Deny a tag ─────────────────────────────────

            app.MapPost("/api/dev/tags/{tagId:guid}/deny", async (
                Guid tagId,
                ITagSubmissionRepository tagSubmissionRepo,
                ITagService tagService,
                CancellationToken ct) =>
            {
                var submission = await tagSubmissionRepo.GetByIdAsync(tagId, ct);
                if (submission is null)
                {
                    return Results.NotFound(new { error = "Tag submission not found." });
                }

                var result = await tagService.DenyTagAsync(submission.TargetId, tagId, ct);
                return Results.Ok(result);
            });

            // ── Dev Quick-Action — End a game ─────────────────────────────────

            devGames.MapPost("/end", async (
                Guid gameId,
                IGameRepository gameRepo,
                IGamePlayerRepository gamePlayerRepo,
                IGameService gameService,
                CancellationToken ct) =>
            {
                var game = await gameRepo.GetByIdAsync(gameId, ct);
                if (game is null)
                {
                    return Results.NotFound(new { error = "Game not found." });
                }

                var memberships = await gamePlayerRepo.GetByGameIdAsync(gameId, ct);
                var creator = memberships.FirstOrDefault(m => m.Role == GameRole.Creator);
                if (creator is null)
                {
                    return Results.NotFound(new { error = "Game creator not found." });
                }

                var result = await gameService.EndGameAsync(creator.PlayerId, gameId, ct);
                return Results.Ok(result);
            });
        }

        app.MapHub<GameHub>("/hubs/game");
    }

    /// <summary>Requires the endpoint to be invoked with a valid bearer token.</summary>
    public static TBuilder RequirePlayer<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(new RequirePlayerMetadata());
        return builder;
    }

    /// <summary>Gets the authenticated player identifier from the current request.</summary>
    public static Guid GetRequiredPlayerId(this HttpContext context)
    {
        if (context.Items[AuthenticationMiddleware.PlayerIdItemKey] is Guid playerId)
        {
            return playerId;
        }

        throw new UnauthorizedException("Authentication is required for this operation.");
    }

    /// <summary>Gets the authenticated player DTO from the current request.</summary>
    public static PlayerDto GetAuthenticatedPlayer(this HttpContext context)
    {
        if (context.Items[AuthenticationMiddleware.PlayerItemKey] is PlayerDto player)
        {
            return player;
        }

        throw new UnauthorizedException("Authentication is required for this operation.");
    }
}
