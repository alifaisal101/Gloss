using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.MergeRequests.BusinessRules;

public sealed class MergeRequestDiffNotTooLarge(string diff) : IBusinessRule
{
    private const int MaxLength = 50_000;

    public DomainError Check() =>
        diff.Length > MaxLength ? MergeRequestErrors.DiffTooLarge : DomainError.None;
}
