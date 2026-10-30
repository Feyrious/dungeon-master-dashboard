using System.Net;
using System.Net.Http.Json;

namespace DungeonMasterDashboard.Tests.TestSupport;

// Answers HttpClient requests from a function and records what was sent.
internal sealed class FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    public static FakeHttpHandler Returning(HttpStatusCode status) => new(_ => new HttpResponseMessage(status));

    public static FakeHttpHandler ReturningJson<T>(T body) => new(_ => Json(body));

    public static HttpResponseMessage Json<T>(T body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(body) };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        Requests.Add(new RecordedRequest(
            request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(), request.Content?.Headers.ContentType?.MediaType, body));
        return respond(request);
    }
}

internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Authorization, string? ContentType, byte[]? Body)
{
    public string BodyText => Body is null ? "" : System.Text.Encoding.UTF8.GetString(Body);
}
