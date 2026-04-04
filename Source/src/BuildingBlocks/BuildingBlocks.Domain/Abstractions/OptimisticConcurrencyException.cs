using System.Globalization;

namespace BuildingBlocks.Domain.Abstractions;

public sealed class OptimisticConcurrencyException : Exception
{
    public OptimisticConcurrencyException() { }

    public OptimisticConcurrencyException(string message) : base(message) { }

    public OptimisticConcurrencyException(string message, Exception innerException) : base(message, innerException) { }

    public OptimisticConcurrencyException(string streamId, long expectedVersion, long actualVersion)
        : base(string.Create(CultureInfo.InvariantCulture, $"Concurrency conflict on stream '{streamId}': expected version {expectedVersion} but found {actualVersion}.")) { }
}
