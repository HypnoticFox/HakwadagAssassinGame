using System.Text;
using Blazored.LocalStorage;
using HakwadagAssassinGame.Utils.Config.Validation;
using HakwadagAssassinGame.Web.Client;
using HakwadagAssassinGame.Web.Client.Auth;
using HakwadagAssassinGame.Web.Client.Http;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

#if DEBUG
using var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var response = await httpClient.GetAsync("devsecrets.json");
if (response.IsSuccessStatusCode)
{
    var json = await response.Content.ReadAsStringAsync();
    var devSecrets = new ConfigurationBuilder()
        .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
        .Build();
    builder.Configuration.AddConfiguration(devSecrets);
}
#endif

builder.Services
    .AddOptions<Config>()
    .BindConfiguration("Config")
    .ValidateMiniValidation()
    .ValidateOnStart();

var config = builder.Configuration
    .GetSection("Config")
    .Get<Config>() ?? throw new InvalidOperationException("Config section not found");

builder.Services.AddScoped<CustomAuthorizationMessageHandler>();

builder.Services.AddHttpClient("ServerAPI",
        client => client.BaseAddress = new Uri(config.ApiHostAddress))
    .AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

builder.Services.AddScoped<HttpClient>(sp =>
    sp.GetRequiredService<IHttpClientFactory>()
        .CreateClient("ServerAPI")
);

builder.Services.AddScoped<BlazorHttpClient>();

builder.Services.AddOidcAuthentication(options =>
    {
        options.ProviderOptions.ClientId = config.Auth0.ClientId;
        options.ProviderOptions.Authority = config.Auth0.Authority;
        options.ProviderOptions.ResponseType = "code";
        options.ProviderOptions.AdditionalProviderParameters.Add("audience", config.Auth0.Audience);
        options.UserOptions.NameClaim = "sub";
    })
    .AddAccountClaimsPrincipalFactory<CustomClaimsPrincipalFactory>();

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("Forbidden", policy => { policy.RequireClaim("userRoles", "Forbidden"); });
    options.AddPolicy("Admin",
        policy => { policy.RequireClaim("userRoles", "Admin-Global", "Admin-HakwadagAssassinGame"); });
});

builder.Services.AddMudServices();

builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();