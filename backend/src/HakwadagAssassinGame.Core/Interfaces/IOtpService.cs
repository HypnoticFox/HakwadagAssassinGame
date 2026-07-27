namespace HakwadagAssassinGame.Core.Interfaces;

/// <summary>Sends and verifies one-time passwords without exposing a delivery implementation.</summary>
public interface IOtpService
{
    /// <summary>Sends a one-time password to an email address.</summary>
    Task SendOtpAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Verifies a one-time password for an email address.</summary>
    Task<bool> VerifyOtpAsync(string email, string otp, CancellationToken cancellationToken = default);
}
