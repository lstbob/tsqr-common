using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TSQR.Common.Exceptions;

namespace TSQR.Common.WebApi;

/// <summary>
/// Maps unhandled exceptions to RFC 7807 problem details. Optimistic
/// concurrency conflicts are mapped to HTTP 409; everything else becomes a
/// generic HTTP 500 without leaking implementation details.
/// </summary>
public sealed class TsqrExceptionHandler(ILogger<TsqrExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is IConcurrencyConflictException conflict)
        {
            logger.LogWarning(
                exception,
                "Optimistic concurrency conflict on {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);

            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status409Conflict,
                "Concurrency conflict",
                exception.Message,
                "https://httpstatuses.com/409",
                conflict.EntityName,
                cancellationToken);

            return true;
        }

        logger.LogError(
            exception,
            "Unhandled exception on {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        await WriteProblemAsync(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            "An unexpected error occurred.",
            "https://httpstatuses.com/500",
            entityName: null,
            cancellationToken);

        return true;
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string type,
        string? entityName,
        CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        if (!string.IsNullOrWhiteSpace(entityName))
            problem.Extensions["entity"] = entityName;

        await context.Response.WriteAsJsonAsync(problem, cancellationToken);
    }
}
