using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Application.Interfaces;
using HakwadagAssassinGame.Application.Services;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Interfaces;
using NSubstitute;

namespace HakwadagAssassinGame.Tests.Unit.Application;

public sealed class AuthServiceTests
{
    private readonly IOtpService otpService = Substitute.For<IOtpService>();
    private readonly IPlayerRepository playerRepository = Substitute.For<IPlayerRepository>();
    private readonly ITokenStore tokenStore = Substitute.For<ITokenStore>();
    private readonly AuthService sut;

    public AuthServiceTests()
    {
        sut = new AuthService(otpService, playerRepository, tokenStore);
    }

    // ── SendOtpAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SendOtpAsync_ValidEmail_TrimsAndDelegates()
    {
        var email = "  test@example.com  ";

        await sut.SendOtpAsync(email);

        await otpService.Received(1).SendOtpAsync("test@example.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendOtpAsync_EmptyEmail_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => sut.SendOtpAsync(""));
    }

    [Fact]
    public async Task SendOtpAsync_WhitespaceEmail_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => sut.SendOtpAsync("   "));
    }

    [Fact]
    public async Task SendOtpAsync_NullEmail_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.SendOtpAsync(null!));
    }

    // ── VerifyOtpAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyOtpAsync_ValidOtpAndNewPlayer_CreatesPlayerAndReturnsResponse()
    {
        var email = "test@example.com";
        var code = "123456";
        var trimmedEmail = email;
        var playerId = Guid.NewGuid();

        otpService.VerifyOtpAsync(trimmedEmail, code, Arg.Any<CancellationToken>()).Returns(true);
        playerRepository.GetByEmailAsync(trimmedEmail, Arg.Any<CancellationToken>()).Returns((Player?)null);
        playerRepository.When(x => x.AddAsync(Arg.Any<Player>(), Arg.Any<CancellationToken>()))
            .Do(call => { var p = call.Arg<Player>(); });

        var result = await sut.VerifyOtpAsync(email, code);

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Player.Email);
        await playerRepository.Received(1).AddAsync(Arg.Any<Player>(), Arg.Any<CancellationToken>());
        await tokenStore.Received(1).StoreAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyOtpAsync_ValidOtpAndExistingPlayer_ReturnsResponse()
    {
        var email = "test@example.com";
        var code = "123456";
        var player = Player.Create(email, "testuser", id: Guid.NewGuid());
        var trimmedEmail = email;

        otpService.VerifyOtpAsync(trimmedEmail, code, Arg.Any<CancellationToken>()).Returns(true);
        playerRepository.GetByEmailAsync(trimmedEmail, Arg.Any<CancellationToken>()).Returns(player);

        var result = await sut.VerifyOtpAsync(email, code);

        Assert.NotNull(result);
        Assert.Equal(player.Id, result.Player.Id);
        Assert.Equal("testuser", result.Player.DisplayName);
        await playerRepository.DidNotReceive().AddAsync(Arg.Any<Player>(), Arg.Any<CancellationToken>());
        await tokenStore.Received(1).StoreAsync(Arg.Any<string>(), player.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyOtpAsync_InvalidOtp_ThrowsUnauthorizedException()
    {
        var email = "test@example.com";
        var code = "wrong";

        otpService.VerifyOtpAsync(email, code, Arg.Any<CancellationToken>()).Returns(false);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => sut.VerifyOtpAsync(email, code));
        Assert.Contains("OTP", ex.Message);
        await playerRepository.DidNotReceive().GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyOtpAsync_EmptyEmail_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => sut.VerifyOtpAsync("", "code"));
    }

    [Fact]
    public async Task VerifyOtpAsync_WhitespaceEmail_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => sut.VerifyOtpAsync("   ", "code"));
    }

    [Fact]
    public async Task VerifyOtpAsync_NullEmail_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.VerifyOtpAsync(null!, "code"));
    }

    [Fact]
    public async Task VerifyOtpAsync_EmptyCode_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => sut.VerifyOtpAsync("test@example.com", ""));
    }

    [Fact]
    public async Task VerifyOtpAsync_WhitespaceCode_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => sut.VerifyOtpAsync("test@example.com", "   "));
    }

    [Fact]
    public async Task VerifyOtpAsync_NullCode_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.VerifyOtpAsync("test@example.com", null!));
    }

    [Fact]
    public async Task VerifyOtpAsync_TrimsEmailAndCode()
    {
        var email = "  test@example.com  ";
        var code = "  123456  ";

        otpService.VerifyOtpAsync("test@example.com", "123456", Arg.Any<CancellationToken>()).Returns(true);
        playerRepository.GetByEmailAsync("test@example.com", Arg.Any<CancellationToken>())
            .Returns(Player.Create("test@example.com", "testuser"));

        var result = await sut.VerifyOtpAsync(email, code);

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Player.Email);
    }

    // ── GetMeAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMeAsync_ValidToken_ReturnsPlayerDto()
    {
        var token = "valid-token";
        var playerId = Guid.NewGuid();
        var player = Player.Create("test@example.com", "testuser", id: playerId);

        tokenStore.GetPlayerIdAsync(token, Arg.Any<CancellationToken>()).Returns(playerId);
        playerRepository.GetByIdAsync(playerId, Arg.Any<CancellationToken>()).Returns(player);

        var result = await sut.GetMeAsync(token);

        Assert.NotNull(result);
        Assert.Equal(playerId, result.Id);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task GetMeAsync_NullToken_ReturnsNull()
    {
        var result = await sut.GetMeAsync(null!);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMeAsync_EmptyToken_ReturnsNull()
    {
        var result = await sut.GetMeAsync("");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMeAsync_WhitespaceToken_ReturnsNull()
    {
        var result = await sut.GetMeAsync("   ");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMeAsync_TokenNotFound_ReturnsNull()
    {
        tokenStore.GetPlayerIdAsync("unknown", Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var result = await sut.GetMeAsync("unknown");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMeAsync_PlayerNotFound_ReturnsNull()
    {
        var token = "valid-token";
        var playerId = Guid.NewGuid();

        tokenStore.GetPlayerIdAsync(token, Arg.Any<CancellationToken>()).Returns(playerId);
        playerRepository.GetByIdAsync(playerId, Arg.Any<CancellationToken>()).Returns((Player?)null);

        var result = await sut.GetMeAsync(token);
        Assert.Null(result);
    }
}
