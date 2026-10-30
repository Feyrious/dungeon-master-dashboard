using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using DungeonMasterDashboard.Contracts.GameSystems;
using DungeonMasterDashboard.Contracts.Worlds;

namespace DungeonMasterDashboard.Web.Api;

// Typed client for the backend Web API. Every request carries a token for the current user.
// The user is read from AuthenticationStateProvider rather than HttpContext so this also
// works inside interactive server circuits. Plain endpoints (outside Blazor) pass the user in.
public sealed class ApiClient(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider,
    ApiTokenService tokenService)
{
    // Game systems

    public async Task<IReadOnlyList<GameSystemSettingsDto>> GetAllSystemSettingsAsync(CancellationToken cancellationToken = default) =>
        await GetJsonAsync<List<GameSystemSettingsDto>>("api/systems/settings", cancellationToken) ?? [];

    // Null when the system has no saved changes.
    public Task<GameSystemSettingsDto?> GetSystemSettingsAsync(string systemId, CancellationToken cancellationToken = default) =>
        GetJsonAsync<GameSystemSettingsDto>($"{SystemPath(systemId)}/settings", cancellationToken);

    public Task SaveSystemSettingsAsync(string systemId, GameSystemSettingsDto settings, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"{SystemPath(systemId)}/settings", settings, cancellationToken);

    // Removes all saved changes for the system, including its uploaded image.
    public Task ResetSystemSettingsAsync(string systemId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"{SystemPath(systemId)}/settings", null, cancellationToken);

    public Task UploadSystemImageAsync(string systemId, byte[] data, string contentType, CancellationToken cancellationToken = default) =>
        UploadImageAsync($"{SystemPath(systemId)}/image", data, contentType, cancellationToken);

    public Task DeleteSystemImageAsync(string systemId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"{SystemPath(systemId)}/image", null, cancellationToken);

    // Null when the system has no uploaded image.
    public Task<(byte[] Data, string ContentType)?> GetSystemImageAsync(
        string systemId, ClaimsPrincipal user, CancellationToken cancellationToken = default) =>
        GetImageAsync($"{SystemPath(systemId)}/image", user, cancellationToken);

    // Worlds

    public async Task<IReadOnlyList<WorldDto>> GetWorldsAsync(CancellationToken cancellationToken = default) =>
        await GetJsonAsync<List<WorldDto>>("api/worlds", cancellationToken) ?? [];

    public async Task<WorldDto> CreateWorldAsync(WorldDto world, CancellationToken cancellationToken = default)
    {
        using var response = await SendRequestAsync(HttpMethod.Post, "api/worlds", world, null, cancellationToken);
        return await response.Content.ReadFromJsonAsync<WorldDto>(cancellationToken)
            ?? throw new HttpRequestException("The API returned no world.");
    }

    public Task UpdateWorldAsync(int worldId, WorldDto world, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/worlds/{worldId}", world, cancellationToken);

    public Task DeleteWorldAsync(int worldId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/worlds/{worldId}", null, cancellationToken);

    public Task UploadWorldImageAsync(int worldId, byte[] data, string contentType, CancellationToken cancellationToken = default) =>
        UploadImageAsync($"api/worlds/{worldId}/image", data, contentType, cancellationToken);

    public Task DeleteWorldImageAsync(int worldId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/worlds/{worldId}/image", null, cancellationToken);

    // Null when the world has no uploaded image.
    public Task<(byte[] Data, string ContentType)?> GetWorldImageAsync(
        int worldId, ClaimsPrincipal user, CancellationToken cancellationToken = default) =>
        GetImageAsync($"api/worlds/{worldId}/image", user, cancellationToken);

    // Plumbing

    private static string SystemPath(string systemId) => $"api/systems/{Uri.EscapeDataString(systemId)}";

    // Null when the API answers 404.
    private async Task<T?> GetJsonAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        using var response = await SendRequestAsync(HttpMethod.Get, requestUri, null, null, cancellationToken, allowNotFound: true);
        return response.StatusCode == HttpStatusCode.NotFound
            ? default
            : await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private async Task SendAsync(HttpMethod method, string requestUri, object? body, CancellationToken cancellationToken)
    {
        using var response = await SendRequestAsync(method, requestUri, body, null, cancellationToken);
    }

    private async Task UploadImageAsync(string requestUri, byte[] data, string contentType, CancellationToken cancellationToken)
    {
        var content = new ByteArrayContent(data) { Headers = { ContentType = new MediaTypeHeaderValue(contentType) } };
        using var response = await SendRequestAsync(HttpMethod.Put, requestUri, content, null, cancellationToken);
    }

    private async Task<(byte[] Data, string ContentType)?> GetImageAsync(string requestUri, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        using var response = await SendRequestAsync(HttpMethod.Get, requestUri, null, user, cancellationToken, allowNotFound: true);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return (data, response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    // Sends a request as the given user (or the current one) and turns failures into exceptions.
    // The body is sent as JSON unless it already is HttpContent.
    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpMethod method, string requestUri, object? body, ClaimsPrincipal? user, CancellationToken cancellationToken, bool allowNotFound = false)
    {
        user ??= (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
        using var request = new HttpRequestMessage(method, requestUri)
        {
            Content = body as HttpContent ?? (body is null ? null : JsonContent.Create(body)),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokenService.CreateToken(user)) },
        };

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode || (allowNotFound && response.StatusCode == HttpStatusCode.NotFound))
        {
            return response;
        }

        using (response)
        {
            throw new HttpRequestException(await ErrorMessageAsync(response, cancellationToken), null, response.StatusCode);
        }
    }

    // The message from an API problem response, so it can be shown to the user.
    private static async Task<string> ErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
            var message = problem?.Errors.SelectMany(e => e.Value).FirstOrDefault() ?? problem?.Detail ?? problem?.Title;
            if (message is not null)
            {
                return message;
            }
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or NotSupportedException or InvalidOperationException)
        {
            // Not a problem response; fall back to the status code.
        }

        return $"The API returned {(int)response.StatusCode} {response.ReasonPhrase}.";
    }
}
