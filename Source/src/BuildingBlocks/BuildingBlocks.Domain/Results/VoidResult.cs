using BuildingBlocks.Domain.Errors;

namespace BuildingBlocks.Domain.Results;

public sealed class VoidResult
{
    public DomainError Error { get; }
    public bool IsSuccess => Error == DomainError.None;
    public bool IsFailure => !IsSuccess;

    private VoidResult(DomainError error) => Error = error;

    internal static VoidResult Success() => new(DomainError.None);
    internal static VoidResult Failure(DomainError error) => new(error);

    public static VoidResult FromDomainError(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return Failure(error);
    }

    public static implicit operator VoidResult(DomainError error) => FromDomainError(error);
}