using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Infrastructure;
using HakwadagAssassinGame.Infrastructure.Services;
using HakwadagAssassinGame.Web.Middleware;
using HakwadagAssassinGame.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDevSeedService, DevSeedService>();

builder.Services.AddHostedService<TagTimeoutBackgroundService>();
builder.Services.AddHostedService<AssignmentCooldownBackgroundService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

// Support remote development via cloudflared: add the tunnel frontend URL if set.
var tunnelFrontendUrl = builder.Configuration["CLOUDFLARED_FRONTEND_URL"];
if (!string.IsNullOrWhiteSpace(tunnelFrontendUrl))
{
    allowedOrigins = allowedOrigins.Append(tunnelFrontendUrl).ToArray();
}

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.SetIsOriginAllowed(origin => allowedOrigins.Contains(origin))
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddSignalR(options =>
{
    // Allow tokens to be passed via query string for WebSocket connections
    options.EnableDetailedErrors = true;
});

var app = builder.Build();

app.UseRouting();
app.UseWebSockets();
app.UseCors("Frontend");
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<AuthenticationMiddleware>();

app.MapEndpoints();

app.Run();

// Make the auto-generated Program class visible for test projects using WebApplicationFactory.
public partial class Program
{
}
