using System.ComponentModel.DataAnnotations;

namespace HakwadagAssassinGame.Web.Client;

internal sealed class Config
{
    [Required]
    public string ApiHostAddress { get; init; } = null!;

    [Required]
    public Auth0 Auth0 { get; init; } = null!;
}

internal sealed class Auth0
{
    [Required]
    public string Authority { get; init; } = null!;

    [Required]
    public string ClientId { get; init; } = null!;

    [Required]
    public string Audience { get; init; } = null!;
}
