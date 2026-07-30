using HakwadagAssassinGame.Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HakwadagAssassinGame.Infrastructure.Services;

/// <summary>Sends email via SMTP using MailKit.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions options;
    private readonly ILogger<SmtpEmailSender> logger;

    /// <summary>Initializes the SMTP email sender.</summary>
    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        this.options = options.Value;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);
        cancellationToken.ThrowIfCancellationRequested();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = 10_000; // 10 seconds

        await client.ConnectAsync(options.Host, options.Port, MailKit.Security.SecureSocketOptions.Auto, cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(options.Username) && !string.IsNullOrWhiteSpace(options.Password))
        {
            await client.AuthenticateAsync(options.Username, options.Password, cancellationToken)
                .ConfigureAwait(false);
        }

        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Email sent to {To} with subject \"{Subject}\"", to, subject);
    }
}
