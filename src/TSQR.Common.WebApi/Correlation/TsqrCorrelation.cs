using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace TSQR.Common.WebApi.Correlation;

/// <summary>
/// Shared names and helpers for the correlation ID that is propagated across
/// HTTP calls and written to structured log scopes.
/// </summary>
public static class TsqrCorrelation
{
    /// <summary>Canonical request/response header used by the suite.</summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>Accepted inbound alias for clients/proxies that already use it.</summary>
    public const string AlternateHeaderName = "X-Request-ID";

    /// <summary>Key used to store the correlation ID on <see cref="HttpContext.Items"/>.</summary>
    public const string ItemKey = "TSQR.CorrelationId";

    /// <summary>Log scope key for the correlation ID.</summary>
    public const string LogScopeKey = "CorrelationId";

    /// <summary>Log scope key for the W3C trace ID.</summary>
    public const string TraceIdScopeKey = "TraceId";

    /// <summary>Log scope key for the current span ID.</summary>
    public const string SpanIdScopeKey = "SpanId";

    /// <summary>Log scope key for the authenticated user ID (<c>sub</c>).</summary>
    public const string UserIdScopeKey = "UserId";

    /// <summary>Log scope key for the active community, when supplied.</summary>
    public const string CommunityIdScopeKey = "CommunityId";

    private const int MaxCorrelationIdLength = 128;

    /// <summary>
    /// Returns the correlation ID assigned to the current request, if the
    /// middleware has already run.
    /// </summary>
    public static string? GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(ItemKey, out var value) ? value as string : null;

    /// <summary>
    /// Returns an inbound correlation ID when it is safe to echo, otherwise a
    /// fresh opaque value. Values are bounded to avoid header/log injection.
    /// </summary>
    public static string Resolve(HttpContext context)
    {
        var candidate = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(candidate))
            candidate = context.Request.Headers[AlternateHeaderName].FirstOrDefault();

        return IsSafe(candidate) ? candidate!.Trim() : Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Builds the log scope for a request. The correlation/trace identifiers
    /// are always included when available; user and community identifiers are
    /// added only after authentication has populated <see cref="HttpContext.User"/>.
    /// </summary>
    public static Dictionary<string, object?> CreateLogScope(
        HttpContext context,
        bool includeUserContext)
    {
        var scope = new Dictionary<string, object?>(StringComparer.Ordinal);

        var correlationId = context.GetCorrelationId();
        if (!string.IsNullOrWhiteSpace(correlationId))
            scope[LogScopeKey] = correlationId;

        var activity = Activity.Current;
        if (activity is not null)
        {
            scope[TraceIdScopeKey] = activity.TraceId.ToHexString();
            scope[SpanIdScopeKey] = activity.SpanId.ToHexString();
        }

        if (includeUserContext)
        {
            var userId = FindClaim(context.User, "sub")
                ?? FindClaim(context.User, ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
                scope[UserIdScopeKey] = userId;

            var communityId = FindClaim(context.User, "communityId")
                ?? FindClaim(context.User, "community_id")
                ?? context.Request.Headers["X-Community-ID"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(communityId))
                scope[CommunityIdScopeKey] = communityId;
        }

        return scope;
    }

    private static string? FindClaim(ClaimsPrincipal user, string type) =>
        user.FindFirst(type)?.Value;

    private static bool IsSafe(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaxCorrelationIdLength
        && value.All(IsAllowedCharacter);

    private static bool IsAllowedCharacter(char value) =>
        char.IsLetterOrDigit(value)
        || value is '-' or '_' or '.' or ':' or '+' or '/' or '=';
}
