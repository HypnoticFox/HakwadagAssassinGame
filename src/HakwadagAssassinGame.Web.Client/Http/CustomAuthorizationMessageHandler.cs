using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;

namespace HakwadagAssassinGame.Web.Client.Http;

internal sealed class CustomAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public CustomAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IOptions<Config> config)
        : base(provider, navigation)
    {
        ConfigureHandler(
            new[] { config.Value.ApiHostAddress },
            returnUrl: $"{navigation.BaseUri}authentication/login"
        );
    }
}