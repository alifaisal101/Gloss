using BuildingBlocks.Domain.Errors;
using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Domain.Models.Batching;

public sealed class BatchSize : ValueObject
{
    public const int DefaultValue = 100;
    public const int HardLimit = 1000;

    public int Value { get; }

    private BatchSize(int value) => Value = value;

    public static Result<BatchSize> Create(int value) =>
        value switch
        {
            < 1 => Result.Failure<BatchSize>(new DomainError("Batching.TooSmall", "Batch size must be at least 1.")),
            > HardLimit => Result.Failure<BatchSize>(new DomainError("Batching.TooLarge",
                $"Batch size cannot exceed {HardLimit}.")),
            _ => Result.Success(new BatchSize(value)),
        };

    public static BatchSize Default() => new(DefaultValue);

    public static implicit operator int(BatchSize size)
    {
        ArgumentNullException.ThrowIfNull(size);
        return size.Value;
    }

    public static int ToInt32(BatchSize size)
    {
        ArgumentNullException.ThrowIfNull(size);
        return size.Value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}