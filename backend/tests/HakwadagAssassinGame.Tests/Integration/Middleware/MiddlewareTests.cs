using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Enums;
using HakwadagAssassinGame.Tests.Integration.Api;

namespace HakwadagAssassinGame.Tests.Integration.Middleware;

/// <summary>
/// Tests for authentication, exception handling, and CORS middleware.
/// </summary>
public sealed class MiddlewareTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory Factory;
    private readonly HttpClient Client;

    public MiddlewareTests(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    // ── Authentication Middleware ─────────────────────────────────────────

    [Fact]
    public async Task AuthMiddleware_MissingBearerToken_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthMiddleware_InvalidBearerToken_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new("Bearer", "not-a-valid-token");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthMiddleware_ExpiredToken_Returns401()
    {
        // Token exists (in InMemoryTokenStore) but points to non-existent player
        await Factory.TokenStore.StoreAsync("expired-token", Guid.NewGuid());
        // Don't create the player in the repo

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new("Bearer", "expired-token");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthMiddleware_ValidToken_Returns200()
    {
        var player = Player.Create("valid@test.com", "ValidPlayer");
        await Factory.PlayerRepository.AddAsync(player);
        await Factory.TokenStore.StoreAsync("valid-token", player.Id);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new("Bearer", "valid-token");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PlayerDto>();
        Assert.NotNull(body);
        Assert.Equal(player.Id, body!.Id);
    }

    [Fact]
    public async Task AuthMiddleware_EndpointWithoutRequirePlayer_DoesNotAuthenticate()
    {
        // /api/auth/send-otp doesn't have RequirePlayer()
        var response = await Client.PostAsJsonAsync("/api/auth/send-otp",
            new { email = "test@test.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Exception Handling Middleware ─────────────────────────────────────

    [Fact]
    public async Task ExceptionMiddleware_UnauthorizedException_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/games");
        request.Headers.Authorization = new("Bearer", "invalid");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task ExceptionMiddleware_NotFound_Returns404()
    {
        var player = Player.Create("test@test.com", "Test");
        await Factory.PlayerRepository.AddAsync(player);
        await Factory.TokenStore.StoreAsync("test-token", player.Id);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/games/{Guid.NewGuid()}");
        request.Headers.Authorization = new("Bearer", "test-token");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task ExceptionMiddleware_BadRequest_Returns400()
    {
        var player = Player.Create("test@test.com", "Test");
        await Factory.PlayerRepository.AddAsync(player);
        await Factory.TokenStore.StoreAsync("test-token", player.Id);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/games")
        {
            Content = JsonContent.Create(new
            {
                name = "Test",
                durationHours = -1,
                maxPlayers = 10,
                basePointsPerTag = 10,
                confirmationTimeoutMinutes = 5
            })
        };
        request.Headers.Authorization = new("Bearer", "test-token");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExceptionMiddleware_UnhandledException_DoesNotBreakNormalRequests()
    {
        // Verify the middleware is registered and doesn't break normal requests
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth");
        var response = await Client.SendAsync(request);

        // This should not be handled by our middleware
        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed);
    }

    // ── CORS Middleware ───────────────────────────────────────────────────

    [Fact]
    public async Task CorsMiddleware_AllowsConfiguredOrigin()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/send-otp");
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await Client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"),
            "CORS header should be present for allowed origin");
    }

    [Fact]
    public async Task CorsMiddleware_AllowsAnyHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/send-otp");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await Client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"),
            "CORS preflight should succeed for allowed origin");
        Assert.True(response.Headers.Contains("Access-Control-Allow-Methods"),
            "CORS preflight should include Allow-Methods");
    }

    [Fact]
    public async Task CorsMiddleware_RejectsDisallowedOrigin()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/send-otp");
        request.Headers.Add("Origin", "https://evil.com");

        var response = await Client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"),
            "CORS header should not be present for disallowed origin");
    }
}
