using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TSQR.Common.WebApi.Correlation;

namespace TSQR.Common.WebApi.Tests;

public class CorrelationTests
{
    [Fact]
    public async Task Middleware_EchoesSafeInboundCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[TsqrCorrelation.HeaderName] = "client-correlation-123";
        var nextCalled = false;

        var middleware = new CorrelationIdMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal("client-correlation-123", context.GetCorrelationId());
        Assert.Equal(
            "client-correlation-123",
            context.Response.Headers[TsqrCorrelation.HeaderName].ToString());
    }

    [Fact]
    public async Task Middleware_UsesAlternateHeaderWhenCanonicalHeaderIsMissing()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[TsqrCorrelation.AlternateHeaderName] = "proxy-request-456";

        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("proxy-request-456", context.GetCorrelationId());
    }

    [Fact]
    public async Task Middleware_GeneratesCorrelationIdWhenHeaderIsMissingOrUnsafe()
    {
        var missing = new DefaultHttpContext();
        var unsafeValue = new DefaultHttpContext();
        unsafeValue.Request.Headers[TsqrCorrelation.HeaderName] = "contains spaces";

        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(missing);
        await middleware.InvokeAsync(unsafeValue);

        Assert.True(Guid.TryParseExact(missing.GetCorrelationId(), "N", out _));
        Assert.True(Guid.TryParseExact(unsafeValue.GetCorrelationId(), "N", out _));
    }

    [Fact]
    public void LogScope_IncludesUserAndCommunityAfterAuthentication()
    {
        var context = new DefaultHttpContext();
        context.Items[TsqrCorrelation.ItemKey] = "correlation-xyz";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", "11111111-1111-1111-1111-111111111111"),
                new Claim("community_id", "42"),
            ],
            "test"));

        var scope = TsqrCorrelation.CreateLogScope(context, includeUserContext: true);

        Assert.Equal("correlation-xyz", scope[TsqrCorrelation.LogScopeKey]);
        Assert.Equal(
            "11111111-1111-1111-1111-111111111111",
            scope[TsqrCorrelation.UserIdScopeKey]);
        Assert.Equal("42", scope[TsqrCorrelation.CommunityIdScopeKey]);
    }

    [Fact]
    public async Task Handler_AddsCorrelationIdToOutgoingRequest()
    {
        var context = new DefaultHttpContext();
        context.Items[TsqrCorrelation.ItemKey] = "outbound-correlation";
        var accessor = new HttpContextAccessor { HttpContext = context };
        var inner = new CapturingHandler();
        var handler = new CorrelationIdHandler(accessor) { InnerHandler = inner };
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://downstream/api");

        await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(
            "outbound-correlation",
            string.Join(",", inner.Request!.Headers.GetValues(TsqrCorrelation.HeaderName)));
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
