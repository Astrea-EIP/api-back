using System.Net;

namespace proto_back.Tests.Support;

/// <summary>
/// Minimal <see cref="HttpMessageHandler"/> stub for unit-testing code that depends on
/// <see cref="HttpClient"/> directly, without any real network call. Records the last
/// request so tests can assert on the URL/headers that were sent.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public HttpRequestMessage? LastRequest { get; private set; }
    public int CallCount { get; private set; }

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public static StubHttpMessageHandler ReturningJson(HttpStatusCode statusCode, string json)
    {
        return new StubHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        CallCount++;
        return Task.FromResult(_responder(request));
    }
}
