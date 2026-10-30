using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;

namespace DungeonMasterDashboard.Web.Api;

// Typed client for the backend Web API. Every request carries a token for the current user.
// The user is read from AuthenticationStateProvider rather than HttpContext so this also
// works inside interactive server circuits.
public sealed class ApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider,
    ApiTokenService tokenService)
{
    public async Task<CurrentUserResponse?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "api/me");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CurrentUserResponse>(cancellationToken);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string requestUri)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.CreateToken(authState.User));
        return request;
    }
}

public sealed record CurrentUserResponse(string Id, string Name, string? Email);
