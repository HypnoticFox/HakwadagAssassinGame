using HakwadagAssassinGame.Application.Commands.ApplicationUsers;
using HakwadagAssassinGame.Application.Queries.ApplicationUsers;

namespace HakwadagAssassinGame.Web.API.Modules;

public sealed class ApplicationUserModule : CarterModule
{
    public ApplicationUserModule()
        : base("/ApplicationUser")
    {
        WithTags("ApplicationUser Module");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/displayname", async (HttpContext context, IApplicationUserQueries queries) =>
            {
                var userId = context.User.Identity?.Name;

                if (userId is null) return Results.Unauthorized();

                var result = await queries.GetUserDisplayNameAsync(userId);
                return result.ToMinimalApiResult();
            })
            .Produces<string>(contentType: "text/plain")
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .ProducesProblem(422)
            .WithName("GetUserDisplayName")
            .WithSummary("Gets the user's display name")
            .WithDescription("Returns the user's display name");

        app.MapPost("/displayname",
                async (HttpContext context, IMediator mediator, IApplicationUserQueries queries, string displayName) =>
                {
                    var userId = context.User.Identity?.Name;

                    if (userId is null) return Results.Unauthorized();

                    var result = await mediator.Send(new SetDisplayNameCommand
                    {
                        UserId = userId,
                        DisplayName = displayName
                    });

                    return result.ToMinimalApiResult();
                })
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .ProducesProblem(422)
            .WithName("SetUserDisplayName")
            .WithSummary("Sets the user's display name")
            .WithDescription("Changes the user's display name to the provided value");
    }
}