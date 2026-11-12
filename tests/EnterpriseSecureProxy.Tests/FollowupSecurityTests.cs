using System.Net;
using System.Net.Http.Json;
using System.Text;
using EnterpriseSecureProxy.Tests.Support;
using Xunit;

namespace EnterpriseSecureProxy.Tests;

/// <summary>Follow-up verification: role-spoofing fallback and global request-size floor.</summary>
[Collection("WebFactory")]
public sealed class FollowupSecurityTests
{
    [Fact]
    public async Task Production_SelfAssertedClientRole_IsNotHonored_AndRequestRejected()
    {
        using var factory = new ProxyWebApplicationFactory("Production");
        using var client = factory.CreateClient();

        // target.location requires a role; X-Client-Role must be ignored outside Development.
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/proxy/execute")
        {
            Content = JsonContent.Create(new
            {
                clientId = "target.location",
                body = new { format = "MGRS", value = "31U" }
            })
        };
        request.Headers.Add("X-Client-Role", "operator");
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Development_SelfAssertedClientRole_IsHonored()
    {
        using var factory = new ProxyWebApplicationFactory("Development");
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/proxy/execute")
        {
            Content = JsonContent.Create(new
            {
                clientId = "target.location",
                body = new { format = "MGRS", value = "31U" }
            })
        };
        request.Headers.Add("X-Client-Role", "operator");
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.SendAsync(request);

        // Role accepted in Development, so it is not a 403; the request proceeds past RBAC.
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OversizedBody_RejectedByGlobalFloor_BeforeOperationLookup()
    {
        using var factory = new ProxyWebApplicationFactory("Production", new Dictionary<string, string?>
        {
            ["RequestLimits:MaxGlobalBodyBytes"] = "1024"
        });
        using var client = factory.CreateClient();

        var huge = new string('x', 8192);
        var payload = $"{{\"clientId\":\"does-not-resolve\",\"parameters\":{{\"blob\":\"{huge}\"}}}}";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/proxy/execute", content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }
}
