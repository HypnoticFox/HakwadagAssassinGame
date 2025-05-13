using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace HakwadagAssassinGame.Web.Client.Http;

public sealed class BlazorHttpClient
{
    private readonly NavigationManager _navigationManager;

    public BlazorHttpClient(HttpClient httpClient, NavigationManager navigationManager)
    {
        HttpClient = httpClient;
        _navigationManager = navigationManager;
    }

    public HttpClient HttpClient { get; }

    public Task<TValue?> GetFromJsonAsync<TValue>(string url)
    {
        var requestTask = HttpClient.GetFromJsonAsync<TValue>(url);
        return AwaitHttpRequest(requestTask);
    }

    public Task<HttpResponseMessage?> PostAsJsonAsync<TValue>(string url, TValue value)
    {
        var requestTask = HttpClient.PostAsJsonAsync(url, value);
        return AwaitHttpRequest(requestTask);
    }

    public async Task<TValue?> AwaitHttpRequest<TValue>(Task<TValue> request)
    {
        try
        {
            return await request;
        }
        catch (AccessTokenNotAvailableException exception)
        {
            exception.Redirect();
        }
        catch (HttpRequestException exception)
        {
            if (exception.StatusCode is HttpStatusCode.Unauthorized)
                _navigationManager.NavigateToLogin("authentication/login");
            else if (exception.StatusCode is HttpStatusCode.Forbidden) _navigationManager.NavigateTo("unauthorized");
        }

        return default;
    }
}