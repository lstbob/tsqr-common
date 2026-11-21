namespace TSQR.Common.DomainKernel;

/// <summary>
/// Marker interface for an aggregate root. Repositories and units of work
/// should treat aggregate roots as the consistency boundary.
/// </summary>
public interface IAggregateRoot
{
}
