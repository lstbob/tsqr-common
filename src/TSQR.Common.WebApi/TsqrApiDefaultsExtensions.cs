using System.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TSQR.Common.WebApi.Correlation;
using TSQR.Common.WebApi.HealthChecks;

namespace TSQR.Common.WebApi;

public static class TsqrApiDefaultsExtensions
{
    /// <summary>
    /// Registers the suite-wide hosting defaults: JWT authentication, a
    /// deny-by-default authorization fallback, RFC 7807 problem details,
    /// controllers, rate limiting, OpenTelemetry and PostgreSQL health checks.
    /// </summary>
    public static WebApplicationBuilder AddTsqrApiDefaults(
        this WebApplicationBuilder builder,
        Action<TsqrApiDefaultsOptions>? configure = null)
    {
        var options = new TsqrApiDefaultsOptions
        {
            ServiceName = builder.Environment.ApplicationName,
        };
        configure?.Invoke(options);

        builder.Services.AddSingleton(options);

        if (options.UseJsonConsoleLogging && !builder.Environment.IsDevelopment())
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddJsonConsole(json =>
            {
                json.IncludeScopes = true;
                json.UseUtcTimestamp = true;
                json.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
            });
        }

        builder.Services.AddTsqrCorrelation();

        builder.Services.AddTsqrJwtAuthentication(builder.Configuration);
        builder.Services.AddAuthorization(authorization =>
        {
            authorization.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.ConfigureAuthorization?.Invoke(authorization);
        });

        builder.Services.AddControllers();
        builder.Services.AddProblemDetails(problemDetails =>
        {
            problemDetails.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

                var correlationId = context.HttpContext.GetCorrelationId();
                if (!string.IsNullOrWhiteSpace(correlationId))
                    context.ProblemDetails.Extensions["correlationId"] = correlationId;
            };
        });
        builder.Services.AddExceptionHandler<TsqrExceptionHandler>();

        if (options.EnableOpenApi)
            builder.Services.AddOpenApi();

        var healthChecks = builder.Services.AddHealthChecks();
        if (options.AddDatabaseHealthCheck)
        {
            var connectionString = builder.Configuration
                .GetConnectionString(options.ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{options.ConnectionStringName}' not found.");

            healthChecks.AddCheck(
                "database",
                new NpgsqlHealthCheck(connectionString),
                tags: new[] { "ready" },
                timeout: TimeSpan.FromSeconds(5));
        }

        if (options.EnableRateLimiting)
        {
            builder.Services.AddRateLimiter(rateLimiter =>
            {
                rateLimiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                rateLimiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    httpContext =>
                    {
                        var path = httpContext.Request.Path;
                        if (path.StartsWithSegments(options.HealthPath) ||
                            path.StartsWithSegments(options.ReadyHealthPath))
                        {
                            return RateLimitPartition.GetNoLimiter("health");
                        }

                        var partitionKey =
                            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey,
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = options.RateLimitPermitLimit,
                                Window = options.RateLimitWindow,
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            });
                    });
            });
        }

        if (options.EnableOpenTelemetry)
        {
            var otlpEndpoint = builder.Configuration["OpenTelemetry:Endpoint"]
                ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

            builder.Services
                .AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(options.ServiceName))
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddSource(options.ServiceName);

                    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    {
                        tracing.AddOtlpExporter(exporter =>
                            exporter.Endpoint = new Uri(otlpEndpoint));
                    }
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();

                    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    {
                        metrics.AddOtlpExporter(exporter =>
                            exporter.Endpoint = new Uri(otlpEndpoint));
                    }
                });

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                builder.Logging.AddOpenTelemetry(logging =>
                {
                    logging.IncludeFormattedMessage = true;
                    logging.IncludeScopes = true;
                    logging.ParseStateValues = true;
                    logging.AddOtlpExporter(exporter =>
                        exporter.Endpoint = new Uri(otlpEndpoint));
                });
            }
        }

        return builder;
    }

    /// <summary>
    /// Wires up the middleware and endpoints registered by
    /// <see cref="AddTsqrApiDefaults"/>.
    /// </summary>
    public static WebApplication UseTsqrApiDefaults(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<TsqrApiDefaultsOptions>();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();

            if (options.EnableOpenApi)
                app.MapOpenApi().AllowAnonymous();
        }

        app.UseExceptionHandler();
        app.UseTsqrCorrelation();
        app.UseHttpsRedirection();
        app.UseRouting();

        if (options.EnableRateLimiting)
            app.UseRateLimiter();

        app.UseAuthentication();
        app.UseTsqrRequestContextLogging();
        app.UseAuthorization();
        app.MapControllers();

        app.MapHealthChecks(options.HealthPath).AllowAnonymous();
        app.MapHealthChecks(options.ReadyHealthPath, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
        }).AllowAnonymous();

        return app;
    }
}
