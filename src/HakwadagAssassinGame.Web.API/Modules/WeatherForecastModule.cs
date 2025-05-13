using System.Collections.Immutable;
using Carter;
using HakwadagAssassinGame.Web.Contracts.Responses.WeatherForecast;

namespace HakwadagAssassinGame.Web.API.Modules;

public sealed class WeatherForecastModule : CarterModule
{
    private static readonly ImmutableArray<string> summaries = ImmutableArray.Create
    (
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    );

    public WeatherForecastModule()
        : base("/WeatherForecast")
    {
        WithTags("WeatherForecast Module");
        RequireAuthorization("Admin");
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/", () =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                        new WeatherForecast
                        (
                            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            Random.Shared.Next(-20, 55),
                            summaries[Random.Shared.Next(summaries.Length)]
                        ))
                    .ToArray();
                return forecast;
            })
            .Produces<WeatherForecast>()
            .WithName("GetWeatherForecast")
            .WithSummary("Gets the weather forecast")
            .WithDescription("Gets the weather forecast");
    }
}