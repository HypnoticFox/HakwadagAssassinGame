using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Infrastructure;
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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

// Support remote development via zrok2: add the zrok frontend URL if set.
var zrokFrontendUrl = builder.Configuration["ZROK_FRONTEND_URL"];
if (!string.IsNullOrWhiteSpace(zrokFrontendUrl))
{
    allowedOrigins = allowedOrigins.Append(zrokFrontendUrl).ToArray();
}

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));
builder.Services.AddSignalR();

var app = builder.Build();

app.UseRouting();
app.UseCors("Frontend");
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<AuthenticationMiddleware>();

app.MapEndpoints();

app.Run();

// Make the auto-generated Program class visible for test projects using WebApplicationFactory.
public partial class Program
{
}
