using BuildingBlocks.Domain.Errors;

namespace BuildingBlocks.Domain.Models.Batching;

public sealed class BatchReport : ValueObject
{
    public int ProcessedCount { get; }
    public int FailedCount { get; }

    private readonly List<DomainError> _errors;
    public IReadOnlyCollection<DomainError> Errors => _errors.AsReadOnly();

    private BatchReport(int processed, int failed, List<DomainError> errors)
    {
        ProcessedCount = processed;
        FailedCount = failed;
        _errors = errors;
    }

    public static BatchReport Create(int processed, int failed, IEnumerable<DomainError> errors)
    {
        return new BatchReport(processed, failed, errors.ToList());
    }

    public static BatchReport Empty() => new(0, 0, []);

    public BatchReport Merge(BatchReport other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var combinedErrors = new List<DomainError>(_errors);
        combinedErrors.AddRange(other.Errors);
        return new BatchReport(
            ProcessedCount + other.ProcessedCount,
            FailedCount + other.FailedCount,
            combinedErrors
        );
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ProcessedCount;
        yield return FailedCount;
        foreach (var error in _errors) yield return error;
    }
}