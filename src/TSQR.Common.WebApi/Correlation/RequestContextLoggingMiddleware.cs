using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TSQR.Common.WebApi.Correlation;

/// <summary>
/// Adds authenticated user and community information to the request logging
/// scope after authentication has run. Keeping this separate from
/// <see cref="CorrelationIdMiddleware"/> means the correlation ID is available
/// even when authentication itself fails.
/// </summary>
public sealed class RequestContextLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestContextLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var scope = TsqrCorrelation.CreateLogScope(context, includeUserContext: true);
        scope.Remove(TsqrCorrelation.LogScopeKey);
        scope.Remove(TsqrCorrelation.TraceIdScopeKey);
        scope.Remove(TsqrCorrelation.SpanIdScopeKey);

        if (scope.Count == 0)
        {
            await next(context);
            return;
        }

        using (logger.BeginScope(scope))
        {
            await next(context);
        }
    }
}
