using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Web.Extensions;

namespace HakwadagAssassinGame.Web.Middleware;

/// <summary>Resolves bearer tokens for endpoints marked as requiring a player.</summary>
public sealed class AuthenticationMiddleware
{
    /// <summary>HttpContext item key containing the authenticated player ID.</summary>
    public const string PlayerIdItemKey = "PlayerId";

    /// <summary>HttpContext item key containing the authenticated player DTO.</summary>
    public const string PlayerItemKey = "Player";

    private readonly RequestDelegate next;

    /// <summary>Initializes the authentication middleware.</summary>
    public AuthenticationMiddleware(RequestDelegate next) => this.next = next;

    /// <summary>Authenticates the request when its endpoint requires a player.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<RequirePlayerMetadata>() is null)
        {
            await next(context);
            return;
        }

        var authorization = context.Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await UnauthorizedAsync(context);
            return;
        }

        var token = authorization["Bearer ".Length..].Trim();
        var authService = context.RequestServices.GetRequiredService<IAuthService>();
        var player = await authService.GetMeAsync(token, context.RequestAborted);
        if (player is null)
        {
            await UnauthorizedAsync(context);
            return;
        }

        context.Items[PlayerIdItemKey] = player.Id;
        context.Items[PlayerItemKey] = player;
        await next(context);
    }

    private static async Task UnauthorizedAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(
            new { error = "A valid bearer token is required." },
            context.RequestAborted);
    }
}
