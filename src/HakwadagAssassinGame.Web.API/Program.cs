using FluentValidation;
using HakwadagAssassinGame.Application.Commands.ApplicationUsers;
using HakwadagAssassinGame.Application.Mediator;
using HakwadagAssassinGame.Application.Queries.ApplicationUsers;
using HakwadagAssassinGame.Domain.Aggregates;
using HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;
using HakwadagAssassinGame.Infrastructure;
using HakwadagAssassinGame.Infrastructure.Repositories;
using HakwadagAssassinGame.Utils.Config;
using HakwadagAssassinGame.Utils.Config.Validation;
using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ZiggyCreatures.Caching.Fusion;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services
    .AddOptions<Config>()
    .BindConfiguration("Config")
    .ValidateMiniValidation()
    .ValidateOnStart();

var config = builder.Configuration
    .GetSection("Config")
    .Get<Config>() ?? throw new InvalidOperationException("Config section not found");

services.AddCarter();
services.AddHealthChecks();

services.AddValidatorsFromAssemblyContaining<CreateApplicationUserCommandValidator>();

services.AddMediatR(cfg =>
{
    cfg.NotificationPublisher = new TaskWhenAllPublisher();
    cfg.RegisterServicesFromAssemblyContaining<CreateApplicationUserCommand>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ApplicationDatabase")));

services.AddTransient<IApplicationQueryables, ApplicationDbQueryables>();

services.AddTransient<IApplicationUserRepository, ApplicationUserRepository>();

services.AddTransient<IApplicationUserQueries, ApplicationUserQueries>();

services.AddMemoryCache();
services.AddFusionCache()
    .TryWithAutoSetup()
    .WithDefaultEntryOptions(new FusionCacheEntryOptions
    {
        Duration = TimeSpan.FromMinutes(5),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromHours(24),
        FailSafeThrottleDuration = TimeSpan.FromSeconds(30)
    });

services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        BearerFormat = "JWT",
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                TokenUrl = new Uri($"https://{config.Auth0.Domain}/oauth/token"),
                AuthorizationUrl = new Uri($"https://{config.Auth0.Domain}/authorize?audience={config.Auth0.Audience}"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "OpenId" },
                    { "profile", "Profile" }
                },
            }
        }
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            ["openid", "profile"]
        }
    });
});

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority = $"https://{config.Auth0.Domain}";
        options.Audience = config.Auth0.Audience;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = config.Auth0.Audience,
            ValidIssuer = $"https://{config.Auth0.Domain}",
            NameClaimType = "sub"
        };

        options.MapInboundClaims = false;

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                await using var scope = context.HttpContext.RequestServices.CreateAsyncScope();

                var claimsPrincipal =
                    context.Principal ?? throw new InvalidOperationException("ClaimsPrincipal not found");

                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var userId = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == "sub")?.Value ??
                             throw new InvalidOperationException("'sub' claim not found");
                var userNickName = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == "nickname")?.Value ??
                                   throw new InvalidOperationException("'nickname' claim not found");

                await mediator.Send(new CreateApplicationUserCommand
                {
                    UserId = userId,
                    DisplayName = userNickName
                });
            }
        };
    });

services.AddAuthorizationBuilder()
    .AddPolicy("Forbidden", policy => { policy.RequireClaim("userRoles", "Forbidden"); })
    .AddPolicy("Admin", policy => { policy.RequireClaim("userRoles", "Admin-Global", "Admin-HakwadagAssassinGame"); })
    ;

// Cors
services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .WithOrigins(config.AllowedOrigins.Split(";"))
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

if (builder.Environment.IsDevelopment())
{
    // OpenTelemetry
    builder.Logging.AddOpenTelemetry(options =>
    {
        options.IncludeScopes = true;
        options.IncludeFormattedMessage = true;
    });

    services.AddOpenTelemetry()
        .WithMetrics(x =>
        {
            x.AddRuntimeInstrumentation()
                .AddMeter(
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http"
                )
                .AddFusionCacheInstrumentation();
        })
        .WithTracing(x =>
        {
            if (builder.Environment.IsDevelopment()) x.SetSampler<AlwaysOnSampler>();

            x.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddFusionCacheInstrumentation();
        })
        .UseOtlpExporter();
}

var app = builder.Build();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1.0");
        options.DocumentTitle = "Hakwadag Assassin Game API";

        if (config.SwaggerOauth != null)
        {
            options.OAuthClientId(config.SwaggerOauth.ClientId);
            options.OAuthClientSecret(config.SwaggerOauth.ClientSecret);
            options.OAuthUsePkce();
        }
    });
}

app.Run();