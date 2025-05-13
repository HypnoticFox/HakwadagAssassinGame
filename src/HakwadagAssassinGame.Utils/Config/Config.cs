using System.ComponentModel.DataAnnotations;

namespace HakwadagAssassinGame.Utils.Config;
public sealed class Config
{
    [Required]
    public string AllowedOrigins { get; init; } = null!;

    [Required]
    public Auth0Config Auth0 { get; init; } = null!;

    // from VisualStudio User Secrets while in local development
    public SwaggerOauthConfig? SwaggerOauth { get; init; }

}

public sealed class Auth0Config
{
    [Required]
    public string Domain { get; init; } = null!;

    [Required]
    public string Audience { get; init; } = null!;
}

public sealed class SwaggerOauthConfig
{
    [Required]
    public string ClientId { get; init; } = null!;

    [Required]
    public string ClientSecret { get; init; } = null!;
}