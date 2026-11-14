using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace TSQR.Common.WebApi.HealthChecks;

/// <summary>
/// Shallow PostgreSQL readiness check. Uses the Npgsql version resolved by the
/// host so services are free to pin Npgsql 9.x or 10.x.
/// </summary>
public sealed class NpgsqlHealthCheck(string connectionString) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.", exception);
        }
    }
}
