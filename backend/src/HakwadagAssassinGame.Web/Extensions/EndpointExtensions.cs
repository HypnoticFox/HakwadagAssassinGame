using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Application.Services;
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
