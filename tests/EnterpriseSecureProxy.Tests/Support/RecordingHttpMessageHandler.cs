using System.Net;

namespace EnterpriseSecureProxy.Tests.Support;

public sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    public int InvocationCount { get; private set; }

    public List<HttpRequestMessage> Requests { get; } = new();

    public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } = _ =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"ok":true}""", System.Text.Encoding.UTF8, "application/json")
        };

    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? ResponderAsync { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        InvocationCount++;
        Requests.Add(request);
        if (ResponderAsync is not null)
        {
            return await ResponderAsync(request, cancellationToken);
        }

        return Responder(request);
    }
}
