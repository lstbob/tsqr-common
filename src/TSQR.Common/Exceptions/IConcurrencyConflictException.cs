namespace TSQR.Common.Exceptions;

/// <summary>
/// Marker interface for optimistic-concurrency conflicts. Web API hosts can
/// map any exception implementing this interface to HTTP 409 without taking a
/// dependency on a particular domain project.
/// </summary>
public interface IConcurrencyConflictException
{
    /// <summary>The table or entity whose row version did not match.</summary>
    string EntityName { get; }
}
