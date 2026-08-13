using System.Net.Http.Headers;
using System.Net.Http.Json;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HakwadagAssassinGame.Tests.Integration.Api;

/// <summary>
/// Base class for API endpoint integration tests.
/// Provides helpers to authenticate, seed data, and make requests
/// against a WebApplicationFactory with in-memory service implementations.
/// </summary>
public abstract class ApiTestBase : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    // Shortcuts to in-memory repositories from the factory
    protected InMemoryGameRepository GameRepo => Factory.GameRepository;
    protected InMemoryPlayerRepository PlayerRepo => Factory.PlayerRepository;
    protected InMemoryGamePlayerRepository GamePlayerRepo => Factory.GamePlayerRepository;
    protected InMemoryAssignmentRepository AssignmentRepo => Factory.AssignmentRepository;
    protected InMemoryTagSubmissionRepository TagRepo => Factory.TagSubmissionRepository;
    protected InMemoryConditionLibrary ConditionLib => Factory.ConditionLibrary;
    protected InMemoryOtpService OtpService => Factory.OtpService;
    protected InMemoryTokenStore TokenStore => Factory.TokenStore;

    protected ApiTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();

        // Reset in-memory stores by clearing service registrations
        // Since repositories are singletons in the factory, we need to clear them.
        // The cleanest approach: factories create new instances for each test class,
        // and the IClassFixture reuses the same factory. But each test in a class
        // gets a fresh Client. The in-memory state persists across tests in the
        // same class, which is fine since each test sets up its own data.

        // Reset mock behaviors
        factory.MockEmailSender.ClearReceivedCalls();
        factory.MockPushService.ClearReceivedCalls();
        factory.MockInviteCodeGenerator.ClearReceivedCalls();

        // Default invite code
        factory.MockInviteCodeGenerator.GenerateCode().Returns("TESTCD");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Seeds a player in the in-memory store and returns it.
    /// </summary>
    protected async Task<Player> SeedPlayerAsync(
        string email = "test@example.com",
        string displayName = "TestPlayer")
    {
        var player = Player.Create(email, displayName);
        await PlayerRepo.AddAsync(player);
        return player;
    }

    /// <summary>
    /// Creates an authentication token for a player.
    /// </summary>
    protected async Task<string> CreateTokenAsync(Player player)
    {
        var token = Guid.NewGuid().ToString("N");
        await TokenStore.StoreAsync(token, player.Id);
        return token;
    }

    /// <summary>
    /// Creates a complete authentication setup: player + token.
    /// </summary>
    protected async Task<(Player Player, string Token)> CreateAuthenticatedPlayerAsync(
        string email = "test@example.com",
        string displayName = "TestPlayer")
    {
        var player = await SeedPlayerAsync(email, displayName);
        var token = await CreateTokenAsync(player);
        return (player, token);
    }

    /// <summary>
    /// Creates an authenticated GET request.
    /// </summary>
    protected async Task<HttpResponseMessage> AuthenticatedGetAsync(string url, string? token = null)
    {
        if (token is null)
        {
            var (_, t) = await CreateAuthenticatedPlayerAsync();
            token = t;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await Client.SendAsync(request);
    }

    /// <summary>
    /// Creates an authenticated POST request with a JSON body.
    /// </summary>
    protected async Task<HttpResponseMessage> AuthenticatedPostAsync(
        string url, object body, string? token = null)
    {
        if (token is null)
        {
            var (_, t) = await CreateAuthenticatedPlayerAsync();
            token = t;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await Client.SendAsync(request);
    }

    /// <summary>
    /// Creates an authenticated DELETE request.
    /// </summary>
    protected async Task<HttpResponseMessage> AuthenticatedDeleteAsync(string url, string? token = null)
    {
        if (token is null)
        {
            var (_, t) = await CreateAuthenticatedPlayerAsync();
            token = t;
        }

        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await Client.SendAsync(request);
    }

    /// <summary>
    /// Creates an authenticated PUT request with a JSON body.
    /// </summary>
    protected async Task<HttpResponseMessage> AuthenticatedPutAsync(
        string url, object body, string? token = null)
    {
        if (token is null)
        {
            var (_, t) = await CreateAuthenticatedPlayerAsync();
            token = t;
        }

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await Client.SendAsync(request);
    }

    /// <summary>
    /// Seeds a game with an optional creator.
    /// </summary>
    protected async Task<Game> SeedGameAsync(
        string name = "TestGame",
        string inviteCode = "TESTCD",
        Player? creator = null,
        GameStatus status = GameStatus.NotStarted)
    {
        creator ??= await SeedPlayerAsync("creator@example.com", "Creator");

        var game = Game.Create(
            name, inviteCode,
            DateTimeOffset.UtcNow.AddDays(7),
            10, 10,
            confirmationTimeout: TimeSpan.FromMinutes(5),
            id: Guid.NewGuid());

        await GameRepo.AddAsync(game);

        var role = status == GameStatus.NotStarted ? GameRole.Creator : GameRole.Player;
        await GamePlayerRepo.AddAsync(GamePlayer.Create(game.Id, creator.Id, role));

        // Adjust game status after creation
        if (status == GameStatus.Active)
        {
            // Game.Start() modifies the Status; we need to serialize/deserialize or
            // recreate with the right status. Simplest: we stored NotStarted, now update.
            game.Start();
            await GameRepo.UpdateAsync(game);
        }
        else if (status == GameStatus.Ended)
        {
            game.Start();
            game.End();
            await GameRepo.UpdateAsync(game);
        }

        return game;
    }
}
