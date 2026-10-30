using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using DungeonMasterDashboard.Contracts;
using DungeonMasterDashboard.Contracts.GameSystems;

namespace DungeonMasterDashboard.Web.Api;

// Typed client for the backend Web API. Every request carries a token for the current user.
// The user is read from AuthenticationStateProvider rather than HttpContext so this also
// works inside interactive server circuits. Plain endpoints (outside Blazor) pass the user in.
public sealed class ApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider,
    ApiTokenService tokenService)
{
    public async Task<CurrentUserResponse?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "api/me");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<CurrentUserResponse>(cancellationToken);
    }

    public async Task<IReadOnlyList<GameSystemSettingsDto>> GetAllSystemSettingsAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "api/systems/settings");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<GameSystemSettingsDto>>(cancellationToken) ?? [];
    }

    // Null when the system has no saved changes.
    public async Task<GameSystemSettingsDto?> GetSystemSettingsAsync(string systemId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"api/systems/{Uri.EscapeDataString(systemId)}/settings");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<GameSystemSettingsDto>(cancellationToken);
    }

    public async Task SaveSystemSettingsAsync(string systemId, GameSystemSettingsDto settings, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Put, $"api/systems/{Uri.EscapeDataString(systemId)}/settings");
        request.Content = JsonContent.Create(settings);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    // Removes all saved changes for the system, including its uploaded image.
    public async Task ResetSystemSettingsAsync(string systemId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Delete, $"api/systems/{Uri.EscapeDataString(systemId)}/settings");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UploadSystemImageAsync(string systemId, byte[] data, string contentType, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Put, $"api/systems/{Uri.EscapeDataString(systemId)}/image");
        request.Content = new ByteArrayContent(data);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteSystemImageAsync(string systemId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Delete, $"api/systems/{Uri.EscapeDataString(systemId)}/image");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    // Null when the system has no uploaded image.
    public async Task<(byte[] Data, string ContentType)?> GetSystemImageAsync(
        string systemId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/systems/{Uri.EscapeDataString(systemId)}/image", user);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return (data, response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string requestUri)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        return CreateRequest(method, requestUri, authState.User);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string requestUri, ClaimsPrincipal user)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.CreateToken(user));
        return request;
    }

    // Turns API problem responses into exceptions with a message that can be shown to the user.
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? message = null;
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
            message = problem?.Errors.SelectMany(e => e.Value).FirstOrDefault() ?? problem?.Detail ?? problem?.Title;
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or NotSupportedException or InvalidOperationException)
        {
            // Not a problem response; fall back to the status code.
        }

        throw new HttpRequestException(message ?? $"The API returned {(int)response.StatusCode} {response.ReasonPhrase}.", null, response.StatusCode);
    }
}
