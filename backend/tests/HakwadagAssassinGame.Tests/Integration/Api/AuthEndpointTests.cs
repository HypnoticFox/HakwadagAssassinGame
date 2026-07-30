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
}
