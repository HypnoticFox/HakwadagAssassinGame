namespace HakwadagAssassinGame.Core.Interfaces;

/// <summary>Sends email messages.</summary>
public interface IEmailSender
{
    /// <summary>Sends an email asynchronously.</summary>
    /// <param name="to">The recipient's email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="htmlBody">The HTML body of the email.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
