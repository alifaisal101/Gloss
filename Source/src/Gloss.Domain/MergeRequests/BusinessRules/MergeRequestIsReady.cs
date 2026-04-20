using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.MergeRequests.BusinessRules;

public sealed class MergeRequestIsReady(MergeRequestState state) : IBusinessRule
{
    public DomainError Check() =>
        state != MergeRequestState.Ready ? MergeRequestErrors.NotReady : DomainError.None;
}
