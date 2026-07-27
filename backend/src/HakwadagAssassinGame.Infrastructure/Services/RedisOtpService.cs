using System.Security.Cryptography;
using HakwadagAssassinGame.Core.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HakwadagAssassinGame.Infrastructure.Services;

/// <summary>Stores short-lived one-time passwords in Redis.</summary>
public sealed class RedisOtpService : IOtpService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
    private readonly IDatabase database;
    private readonly ILogger<RedisOtpService> logger;

    /// <summary>Initializes an OTP service.</summary>
    public RedisOtpService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisOtpService> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
