using HakwadagAssassinGame.Application.Interfaces;
using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HakwadagAssassinGame.Tests.Integration.Api;

/// <summary>
/// Custom WebApplicationFactory that replaces external dependencies (Redis, email, push)
/// with in-memory implementations for API integration testing.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>Gets the in-memory game repository shared across tests.</summary>
    public InMemoryGameRepository GameRepository { get; } = new();

    /// <summary>Gets the in-memory player repository.</summary>
    public InMemoryPlayerRepository PlayerRepository { get; } = new();

    /// <summary>Gets the in-memory game-player repository.</summary>
    public InMemoryGamePlayerRepository GamePlayerRepository { get; } = new();

    /// <summary>Gets the in-memory assignment repository.</summary>
    public InMemoryAssignmentRepository AssignmentRepository { get; } = new();

    /// <summary>Gets the in-memory tag submission repository.</summary>
    public InMemoryTagSubmissionRepository TagSubmissionRepository { get; } = new();

    /// <summary>Gets the in-memory condition library.</summary>
    public InMemoryConditionLibrary ConditionLibrary { get; } = new();

    /// <summary>Gets the in-memory OTP service.</summary>
    public InMemoryOtpService OtpService { get; } = new();

    /// <summary>Gets the in-memory token store.</summary>
    public InMemoryTokenStore TokenStore { get; } = new();

    /// <summary>Gets the mock email sender.</summary>
    public IEmailSender MockEmailSender { get; } = Substitute.For<IEmailSender>();

    /// <summary>Gets the mock push notification service.</summary>
    public IPushNotificationService MockPushService { get; } = Substitute.For<IPushNotificationService>();

    /// <summary>Gets the mock invite code generator.</summary>
    public IInviteCodeGenerator MockInviteCodeGenerator { get; } = Substitute.For<IInviteCodeGenerator>();

    /// <summary>Clears all in-memory data stores.</summary>
    public void ClearData()
    {
        // Replace the collections by clearing them if possible, or create new instances
        // Since they're ConcurrentDictionary-backed, clearing would require reset.
        // We'll let each test dispose and recreate.
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove Redis-based repository registrations
            var typesToReplace = new[]
            {
                typeof(IGameRepository),
                typeof(IPlayerRepository),
                typeof(IGamePlayerRepository),
                typeof(IAssignmentRepository),
                typeof(ITagSubmissionRepository),
                typeof(IConditionLibrary),
                typeof(IOtpService),
                typeof(ITokenStore),
                typeof(IEmailSender),
                typeof(IPushNotificationService),
                typeof(IInviteCodeGenerator),
            };

            foreach (var serviceType in typesToReplace)
            {
                var descriptors = services
                    .Where(d => d.ServiceType == serviceType)
                    .ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }
            }

            // Register in-memory implementations
            services.AddSingleton<IGameRepository>(GameRepository);
            services.AddSingleton<IPlayerRepository>(PlayerRepository);
            services.AddSingleton<IGamePlayerRepository>(GamePlayerRepository);
            services.AddSingleton<IAssignmentRepository>(AssignmentRepository);
            services.AddSingleton<ITagSubmissionRepository>(TagSubmissionRepository);
            services.AddSingleton<IConditionLibrary>(ConditionLibrary);
            services.AddSingleton<IOtpService>(OtpService);
            services.AddSingleton<ITokenStore>(TokenStore);
            services.AddSingleton<IEmailSender>(MockEmailSender);
            services.AddSingleton<IPushNotificationService>(MockPushService);
            services.AddSingleton<IInviteCodeGenerator>(MockInviteCodeGenerator);
        });
    }
}
