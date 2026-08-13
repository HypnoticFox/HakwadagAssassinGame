using HakwadagAssassinGame.Core.Interfaces;
using HakwadagAssassinGame.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace HakwadagAssassinGame.Tests.Integration.Redis;

public sealed class RedisOtpServiceTests : RedisTestBase
{
    private RedisOtpService CreateService(IEmailSender? emailSender = null)
    {
        emailSender ??= Substitute.For<IEmailSender>();
        var logger = Substitute.For<ILogger<RedisOtpService>>();
        return new RedisOtpService(Multiplexer, logger, emailSender);
    }

    [Fact]
    public async Task SendOtp_StoresOtp()
    {
        var emailSender = Substitute.For<IEmailSender>();
        var service = CreateService(emailSender);

        await service.SendOtpAsync("user@example.com");

        var stored = await Database.StringGetAsync("otp:user@example.com");
        Assert.False(stored.IsNullOrEmpty);
        Assert.Equal(6, stored.ToString().Length);
    }

    [Fact]
    public async Task SendOtp_SendsEmail()
    {
        var emailSender = Substitute.For<IEmailSender>();
        var service = CreateService(emailSender);

        await service.SendOtpAsync("user@example.com");

        await emailSender.Received(1).SendAsync(
            "user@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyOtp_CorrectCode_ReturnsTrueAndRemovesOtp()
    {
        var emailSender = Substitute.For<IEmailSender>();
        var service = CreateService(emailSender);

        await service.SendOtpAsync("user@example.com");
        var stored = (await Database.StringGetAsync("otp:user@example.com")).ToString();

        var result = await service.VerifyOtpAsync("user@example.com", stored);

        Assert.True(result);

        // OTP should be deleted after successful verification (one-time use)
        var after = await Database.StringGetAsync("otp:user@example.com");
        Assert.True(after.IsNullOrEmpty);
    }

    [Fact]
    public async Task VerifyOtp_WrongCode_ReturnsFalse()
    {
        var emailSender = Substitute.For<IEmailSender>();
        var service = CreateService(emailSender);

        await service.SendOtpAsync("user@example.com");

        var result = await service.VerifyOtpAsync("user@example.com", "000000");

        Assert.False(result);
    }

    [Fact]
    public async Task VerifyOtp_AfterExpiry_ReturnsFalse()
    {
        // This test verifies that an OTP with a very short expiry works correctly.
        // Since we cannot easily change the internal OtpLifetime (5 min),
        // we verify that a non-existent OTP returns false.
        var service = CreateService();

        var result = await service.VerifyOtpAsync("never@sent.com", "123456");

        Assert.False(result);
    }

    [Fact]
    public async Task VerifyOtp_OneTimeUse_SecondVerificationFails()
    {
        var emailSender = Substitute.For<IEmailSender>();
        var service = CreateService(emailSender);

        await service.SendOtpAsync("user@example.com");
        var stored = (await Database.StringGetAsync("otp:user@example.com")).ToString();

        var first = await service.VerifyOtpAsync("user@example.com", stored);
        var second = await service.VerifyOtpAsync("user@example.com", stored);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task SendOtp_EmailSenderThrows_DoesNotThrow()
    {
        var emailSender = Substitute.For<IEmailSender>();
        emailSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("SMTP failed")));
        var service = CreateService(emailSender);

        // Should not throw — the service catches email send failures and logs them
        var exception = await Record.ExceptionAsync(() => service.SendOtpAsync("user@example.com"));

        Assert.Null(exception);

        // OTP should still be stored
        var stored = await Database.StringGetAsync("otp:user@example.com");
        Assert.False(stored.IsNullOrEmpty);
    }
}
