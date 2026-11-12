using System.Net;
using System.Text.Json;
using EnterpriseSecureProxy.Api.Infrastructure;
using EnterpriseSecureProxy.Application.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseSecureProxy.Tests;

public sealed class ProxyExceptionHandlerTests
{
    [Fact]
    public async Task DbUpdateConcurrencyException_ReturnsStructuredProxyExecuteResponse_With409()
    {
        var handler = new ProxyExceptionHandler(NullLogger<ProxyExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };
        httpContext.Request.Headers["X-Correlation-Id"] = "corr-409";
        httpContext.Request.RouteValues["id"] = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var handled = await handler.TryHandleAsync(
            httpContext,
            new DbUpdateConcurrencyException(),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal((int)HttpStatusCode.Conflict, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var body = await JsonSerializer.DeserializeAsync<ProxyExecuteResponse>(
            httpContext.Response.Body,
            jsonOptions,
            CancellationToken.None);

        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.Equal("corr-409", body.CorrelationId);
        Assert.Equal("CONFIGURATION_CONFLICT", body.Status);
        Assert.Equal("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", body.OperationKey);
    }
}
