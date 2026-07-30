using HakwadagAssassinGame.Application.Interfaces;
using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Core.Interfaces;
using HakwadagAssassinGame.Infrastructure.Persistence.Redis;
using HakwadagAssassinGame.Infrastructure.Realtime;
using HakwadagAssassinGame.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HakwadagAssassinGame.Infrastructure;

/// <summary>Registers infrastructure services and Redis-backed implementations.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Adds Redis persistence, services, and the game SignalR hub.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddScoped<IGameRepository, RedisGameRepository>();
        services.AddScoped<IPlayerRepository, RedisPlayerRepository>();
        services.AddScoped<IGamePlayerRepository, RedisGamePlayerRepository>();
        services.AddScoped<IAssignmentRepository, RedisAssignmentRepository>();
        services.AddScoped<ITagSubmissionRepository, RedisTagSubmissionRepository>();
        services.AddScoped<IConditionLibrary, RedisConditionLibrary>();

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IOtpService, RedisOtpService>();
        services.AddScoped<ITokenStore, RedisTokenStore>();
        services.AddScoped<IInviteCodeGenerator, RandomInviteCodeGenerator>();
        services.AddScoped<IPushNotificationService, WebPushNotificationService>();
        services.AddScoped<INotificationHub, SignalRNotificationHub>();

        services.AddSignalR();
        return services;
    }
}
