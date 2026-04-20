using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.MergeRequests.BusinessRules;

public sealed class MergeRequestHasHeadSha(string? headSha) : IBusinessRule
{
    public DomainError Check() =>
        headSha is null ? MergeRequestErrors.MissingShas : DomainError.None;
}
