namespace HakwadagAssassinGame.Infrastructure.Services;

/// <summary>SMTP configuration for sending email.</summary>
public sealed class SmtpOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Smtp";

    /// <summary>SMTP server hostname.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP server port.</summary>
    public int Port { get; set; } = 465;

    /// <summary>Whether to use SSL/TLS.</summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>SMTP authentication username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SMTP authentication password.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>The sender email address.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>The sender display name.</summary>
    public string FromName { get; set; } = "Hakwadag";
}
