using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TSQR.Common.WebApi.Correlation;

namespace TSQR.Common.WebApi;

/// <summary>
/// Registration and middleware helpers for the suite-wide correlation ID.
/// </summary>
public static class TsqrCorrelationExtensions
{
    /// <summary>
    /// Registers the outbound correlation-ID handler for every
    /// <see cref="HttpClient"/> created by <see cref="IHttpClientFactory"/>.
    /// </summary>
    public static IServiceCollection AddTsqrCorrelation(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdHandler>();
        services.ConfigureHttpClientDefaults(httpClient =>
            httpClient.AddHttpMessageHandler<CorrelationIdHandler>());

        return services;
    }

    /// <summary>
    /// Assigns/echoes the correlation ID. Register this before authentication
    /// so even 401/403 responses carry a correlation ID.
    /// </summary>
    public static IApplicationBuilder UseTsqrCorrelation(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();

    /// <summary>
    /// Adds user and community fields to the log scope. Register this after
    /// authentication and before authorization/endpoints.
    /// </summary>
    public static IApplicationBuilder UseTsqrRequestContextLogging(
        this IApplicationBuilder app) =>
        app.UseMiddleware<RequestContextLoggingMiddleware>();
}
