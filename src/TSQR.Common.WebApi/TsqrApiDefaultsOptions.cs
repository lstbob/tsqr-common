using Microsoft.AspNetCore.Authorization;

namespace TSQR.Common.WebApi;

/// <summary>
/// Options for <see cref="TsqrApiDefaultsExtensions.AddTsqrApiDefaults"/>.
/// </summary>
public sealed class TsqrApiDefaultsOptions
{
    /// <summary>Service name reported to OpenTelemetry.</summary>
    public string ServiceName { get; set; } = "tsqr-service";

    /// <summary>Name of the connection string used by the database health check.</summary>
    public string ConnectionStringName { get; set; } = "DefaultConnection";

    /// <summary>Whether to register a PostgreSQL readiness check.</summary>
    public bool AddDatabaseHealthCheck { get; set; } = true;

    /// <summary>Whether to enable the global rate limiter.</summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>Whether to collect and export OpenTelemetry traces and metrics.</summary>
    public bool EnableOpenTelemetry { get; set; } = true;

    /// <summary>Whether to register the OpenAPI document/services.</summary>
    public bool EnableOpenApi { get; set; } = true;

    /// <summary>Use JSON console logging outside Development.</summary>
    public bool UseJsonConsoleLogging { get; set; } = true;

    /// <summary>Requests allowed per partition within <see cref="RateLimitWindow"/>.</summary>
    public int RateLimitPermitLimit { get; set; } = 600;

    /// <summary>Fixed rate-limit window.</summary>
    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Path for the health endpoint.</summary>
    public string HealthPath { get; set; } = "/api/health";

    /// <summary>Path for the readiness endpoint.</summary>
    public string ReadyHealthPath { get; set; } = "/api/health/ready";

    /// <summary>Additional authorization policies/configuration for the host.</summary>
    public Action<AuthorizationOptions>? ConfigureAuthorization { get; set; }
}
