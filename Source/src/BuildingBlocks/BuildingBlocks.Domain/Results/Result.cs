using System.Diagnostics.CodeAnalysis;
using BuildingBlocks.Domain.Errors;

namespace BuildingBlocks.Domain.Results;

public sealed class Result<T>
{
    private readonly T? _value;
    private readonly DomainError _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsFailure ? throw new InvalidOperationException($"Cannot access Value on Failure Result. Error: {_error.Code}") : _value!;

    public DomainError Error => IsSuccess ? throw new InvalidOperationException("Cannot access Error on Success Result.") : _error;

    internal Result(bool isSuccess, T? value, DomainError error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    public Result(T value) : this(true, value, DomainError.None) { }

    public Result(DomainError error) : this(false, default, error)
    {
        ArgumentNullException.ThrowIfNull(error);
    }

    [SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "Factory methods and implicit operators must live on the generic type; a companion static class cannot define implicit operators for Result<T>.")]
    public static Result<T> ToResult(T value) => new(value);

    [SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "Factory methods and implicit operators must live on the generic type; a companion static class cannot define implicit operators for Result<T>.")]
    public static Result<T> FromDomainError(DomainError error) => new(error);

    public static implicit operator Result<T>(T value) => ToResult(value);
    public static implicit operator Result<T>(DomainError error) => FromDomainError(error);
}

public static class Result
{
    public static Result<T> Success<T>(T value) => new(true, value, DomainError.None);
    public static VoidResult Success() => VoidResult.Success();

    public static Result<T> Failure<T>(DomainError error) => new(false, default, error);
    public static VoidResult Failure(DomainError error) => VoidResult.Failure(error);
}