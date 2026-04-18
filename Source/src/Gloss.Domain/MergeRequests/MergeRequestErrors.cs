using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.MergeRequests;

public static class MergeRequestErrors
{
    public static readonly DomainError RepositoryNotFound =
        new("MergeRequest.Repository.NotFound", "Repository not found.");

    public static readonly DomainError NotFound =
        new("MergeRequest.NotFound", "Merge request not found.");

    public static readonly DomainError GitProviderUnauthorized =
        new("MergeRequest.GitProvider.Rejected", "Git provider rejected the request. Check your access token in Settings.");

    public static readonly DomainError LlmProviderUnauthorized =
        new("MergeRequest.LlmProvider.Rejected", "LLM provider rejected the request. Check your API key in Settings.");

    public static readonly DomainError DiffTooLarge =
        new("MergeRequest.DiffTooLarge", "Diff is too large to review automatically. Break the MR into smaller changes.");

    public static readonly DomainError NotReady =
        new("MergeRequest.NotReady", "Merge request must be in Ready state before publishing.");

    public static readonly DomainError AlreadyReviewing =
        new("MergeRequest.Review.Conflict", "Merge request is already being reviewed.");

    public static readonly DomainError MissingShas =
        new("MergeRequest.MissingShas", "Diff reference SHAs are missing. Re-pull the merge request to refresh them.");

    public static readonly DomainError RepoCloneFailed =
        new("MergeRequest.RepoClone.Failed", "Failed to clone or fetch the repository. Check network access and your Git token.");
}
