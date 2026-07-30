using System.Security.Cryptography;
using HakwadagAssassinGame.Core.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HakwadagAssassinGame.Infrastructure.Services;

/// <summary>Stores short-lived one-time passwords in Redis and optionally sends them via email.</summary>
public sealed class RedisOtpService : IOtpService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
    private readonly IDatabase database;
    private readonly ILogger<RedisOtpService> logger;
    private readonly IEmailSender emailSender;

    /// <summary>Initializes an OTP service.</summary>
    public RedisOtpService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisOtpService> logger,
        IEmailSender emailSender)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        ArgumentNullException.ThrowIfNull(emailSender);
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.emailSender = emailSender;
        database = connectionMultiplexer.GetDatabase();
    }

    /// <inheritdoc />
    public async Task SendOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        cancellationToken.ThrowIfCancellationRequested();
        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        await database.StringSetAsync($"otp:{email}", otp, OtpLifetime).WaitAsync(cancellationToken);
        logger.LogInformation("Development OTP for {Email}: {Otp}", email, otp);
        try
        {
            await SendOtpEmailAsync(email, otp, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send OTP email to {Email}. The OTP is still valid.", email);
        }
    }

    private async Task SendOtpEmailAsync(string email, string otp, CancellationToken cancellationToken)
    {
        const string subject = "Your Hakwadag login code";
        var htmlBody = $"""
            <html><body>
            <h2>Your login code</h2>
            <p style="font-size: 32px; font-weight: bold; letter-spacing: 4px;">{otp}</p>
            <p>This code expires in 5 minutes.</p>
            </body></html>
            """;
        await emailSender.SendAsync(email, subject, htmlBody, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> VerifyOtpAsync(
        string email,
        string otp,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(otp);
        cancellationToken.ThrowIfCancellationRequested();

        var storedOtp = await database.StringGetAsync($"otp:{email}").WaitAsync(cancellationToken);
        if (storedOtp.IsNullOrEmpty || !string.Equals(storedOtp.ToString(), otp, StringComparison.Ordinal))
        {
            return false;
        }

        await database.KeyDeleteAsync($"otp:{email}").WaitAsync(cancellationToken);
        return true;
    }
}
