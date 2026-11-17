using Microsoft.AspNetCore.Http;

namespace TSQR.Common.WebApi.Correlation;

/// <summary>
/// Propagates the current request's correlation ID to outgoing
/// <see cref="HttpClient"/> calls made through <see cref="IHttpClientFactory"/>.
/// </summary>
public sealed class CorrelationIdHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContextAccessor.HttpContext?.GetCorrelationId();

        if (!string.IsNullOrWhiteSpace(correlationId)
            && !request.Headers.Contains(TsqrCorrelation.HeaderName))
        {
            request.Headers.TryAddWithoutValidation(
                TsqrCorrelation.HeaderName,
                correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
