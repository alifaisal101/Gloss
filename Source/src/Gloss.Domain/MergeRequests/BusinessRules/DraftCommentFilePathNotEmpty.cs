using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.MergeRequests.BusinessRules;

public sealed class DraftCommentFilePathNotEmpty(string filePath) : IBusinessRule
{
    public DomainError Check() =>
        string.IsNullOrWhiteSpace(filePath) ? MergeRequestErrors.CommentFilePathRequired : DomainError.None;
}
