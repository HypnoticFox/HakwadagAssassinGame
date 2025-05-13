using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace HakwadagAssassinGame.Web.Client.Auth;

internal sealed class CustomClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
{
    public CustomClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor)
        : base(accessor)
    {
    }

    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(
        RemoteUserAccount account,
        RemoteAuthenticationUserOptions options)
    {
        var user = await base.CreateUserAsync(account, options);

        if (account is null) return user;

        var claimsIdentity = (ClaimsIdentity)(user.Identity ?? throw new InvalidOperationException());
        MapArrayClaimsToMultipleSeparateClaims(account, claimsIdentity);

        return user;
    }

    private static void MapArrayClaimsToMultipleSeparateClaims(RemoteUserAccount account, ClaimsIdentity claimsIdentity)
    {
        foreach (var (key, value) in account.AdditionalProperties)
        {
            if (value is not JsonElement { ValueKind: JsonValueKind.Array } element) continue;

            claimsIdentity.RemoveClaim(claimsIdentity.FindFirst(key));

            var claims = element.EnumerateArray()
                .Select(x => new Claim(key, x.ToString()));

            claimsIdentity.AddClaims(claims);
        }
    }
}