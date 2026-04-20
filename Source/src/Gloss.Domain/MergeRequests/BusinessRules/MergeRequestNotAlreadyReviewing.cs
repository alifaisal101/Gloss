using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.MergeRequests.BusinessRules;

public sealed class MergeRequestNotAlreadyReviewing(MergeRequestState state) : IBusinessRule
{
    public DomainError Check() =>
        state == MergeRequestState.Reviewing ? MergeRequestErrors.AlreadyReviewing : DomainError.None;
}
