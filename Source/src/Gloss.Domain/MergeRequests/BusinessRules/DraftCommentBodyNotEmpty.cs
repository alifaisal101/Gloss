using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.MergeRequests.BusinessRules;

public sealed class DraftCommentBodyNotEmpty(string body) : IBusinessRule
{
    public DomainError Check() =>
        string.IsNullOrWhiteSpace(body) ? MergeRequestErrors.CommentBodyRequired : DomainError.None;
}
