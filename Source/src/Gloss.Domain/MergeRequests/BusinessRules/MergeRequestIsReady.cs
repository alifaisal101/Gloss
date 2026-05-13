using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.MergeRequests.BusinessRules;

public sealed class MergeRequestIsReady(MergeRequestStatus status) : IBusinessRule
{
    public DomainError Check() =>
        status is not (MergeRequestStatus.Ready or MergeRequestStatus.Seen or MergeRequestStatus.Staged)
            ? MergeRequestErrors.NotReady
            : DomainError.None;
}
