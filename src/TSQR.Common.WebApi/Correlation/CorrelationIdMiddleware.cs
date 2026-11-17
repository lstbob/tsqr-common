using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TSQR.Common.WebApi.Correlation;

/// <summary>
/// Assigns a correlation ID to every request, echoes it on the response, adds
/// it to the current OpenTelemetry activity, and pushes it into the logging
/// scope so JSON log entries can be correlated across services.
/// </summary>
public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = TsqrCorrelation.Resolve(context);
        context.Items[TsqrCorrelation.ItemKey] = correlationId;
        context.Response.Headers[TsqrCorrelation.HeaderName] = correlationId;

        var activity = Activity.Current;
        activity?.SetTag("correlation.id", correlationId);
        activity?.SetBaggage("correlation.id", correlationId);

        using (logger.BeginScope(TsqrCorrelation.CreateLogScope(context, includeUserContext: false)))
        {
            await next(context);
        }
    }
}
