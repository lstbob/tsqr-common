namespace TSQR.Common.DomainKernel;

/// <summary>
/// Marker for strongly typed identifiers backed by an integer key. The
/// domain kernel treats non-positive values as transient.
/// </summary>
public interface IIntegerId
{
    int Value { get; }
}
