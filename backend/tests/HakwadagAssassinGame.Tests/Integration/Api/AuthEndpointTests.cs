using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HakwadagAssassinGame.Application.Dtos;

namespace HakwadagAssassinGame.Tests.Integration.Api;

public sealed class AuthEndpointTests : ApiTestBase
{
    public AuthEndpointTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task SendOtp_ValidEmail_Returns200()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/send-otp",
            new SendOtpRequest("user@example.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendOtp_EmptyEmail_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/send-otp",
            new SendOtpRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VerifyOtp_ValidCode_Returns200WithToken()
    {
        // Set a known OTP
        OtpService.SetOtp("user@example.com", "123456");

        var response = await Client.PostAsJsonAsync("/api/auth/verify-otp",
            new VerifyOtpRequest("user@example.com", "123456"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotNull(body);
        Assert.True(body.TryGetProperty("token", out var tokenProp));
        Assert.False(string.IsNullOrWhiteSpace(tokenProp.GetString()));
        Assert.True(body.TryGetProperty("player", out var playerProp));
        Assert.Equal("user", playerProp.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task VerifyOtp_InvalidCode_Returns401()
    {
        OtpService.SetOtp("user@example.com", "999999");

        var response = await Client.PostAsJsonAsync("/api/auth/verify-otp",
            new VerifyOtpRequest("user@example.com", "000000"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task VerifyOtp_EmptyEmail_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/verify-otp",
            new VerifyOtpRequest("", "123456"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_ValidToken_Returns200()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync("me@test.com", "MePlayer");

        var response = await AuthenticatedGetAsync("/api/auth/me", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PlayerDto>();
        Assert.NotNull(body);
        Assert.Equal(player.Id, body.Id);
        Assert.Equal("me@test.com", body.Email);
    }

    [Fact]
    public async Task GetMe_NoToken_Returns401()
    {
        var response = await Client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }


    [Fact]
    public async Task GetMe_InvalidToken_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new("Bearer", "invalid-token");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_TokenForDeletedPlayer_Returns401()
    {
        var player = await SeedPlayerAsync("ghost@test.com", "Ghost");
        var token = await CreateTokenAsync(player);
        // Delete the player from the repository
        await PlayerRepo.DeleteAsync(player.Id);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new("Bearer", token);

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_ValidDisplayName_Returns200WithUpdatedPlayer()
    {
        var (player, token) = await CreateAuthenticatedPlayerAsync("update@test.com", "OldName");

        var response = await AuthenticatedPutAsync("/api/auth/me", new UpdatePlayerRequest("NewName"), token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PlayerDto>();
        Assert.NotNull(body);
        Assert.Equal(player.Id, body.Id);
        Assert.Equal("NewName", body.DisplayName);

        // The change is persisted
        var stored = await PlayerRepo.GetByIdAsync(player.Id);
        Assert.NotNull(stored);
        Assert.Equal("NewName", stored.DisplayName);
    }

    [Fact]
    public async Task UpdateMe_DisplayNameWithSurroundingWhitespace_IsTrimmed()
    {
        var (_, token) = await CreateAuthenticatedPlayerAsync("trim@test.com", "Before");

        var response = await AuthenticatedPutAsync("/api/auth/me", new UpdatePlayerRequest("  TrimmedName  "), token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PlayerDto>();
        Assert.NotNull(body);
        Assert.Equal("TrimmedName", body.DisplayName);
    }

    [Fact]
    public async Task UpdateMe_EmptyDisplayName_Returns400()
    {
        var (_, token) = await CreateAuthenticatedPlayerAsync("empty@test.com", "Player");

        var response = await AuthenticatedPutAsync("/api/auth/me", new UpdatePlayerRequest(""), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_WhitespaceDisplayName_Returns400()
    {
        var (_, token) = await CreateAuthenticatedPlayerAsync("space@test.com", "Player");

        var response = await AuthenticatedPutAsync("/api/auth/me", new UpdatePlayerRequest("   "), token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_NoToken_Returns401()
    {
        var response = await Client.PutAsJsonAsync("/api/auth/me", new UpdatePlayerRequest("NewName"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_InvalidToken_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/auth/me")
        {
            Content = JsonContent.Create(new UpdatePlayerRequest("NewName"))
        };
        request.Headers.Authorization = new("Bearer", "invalid-token");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
