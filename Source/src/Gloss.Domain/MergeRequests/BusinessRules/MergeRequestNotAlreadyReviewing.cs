using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.MergeRequests.BusinessRules;

public sealed class MergeRequestNotAlreadyReviewing(MergeRequestStatus status) : IBusinessRule
{
    public DomainError Check() =>
        status is MergeRequestStatus.Reviewing ? MergeRequestErrors.AlreadyReviewing : DomainError.None;
}
