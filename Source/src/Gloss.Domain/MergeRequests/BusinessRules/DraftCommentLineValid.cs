using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.MergeRequests.BusinessRules;

public sealed class DraftCommentLineValid(int line) : IBusinessRule
{
    public DomainError Check() =>
        line <= 0 ? MergeRequestErrors.CommentLineInvalid : DomainError.None;
}
