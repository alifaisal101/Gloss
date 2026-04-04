using BuildingBlocks.Domain.Errors;

namespace Gloss.Domain.MergeRequests;

public static class MergeRequestErrors
{
    public static readonly DomainError RepositoryNotFound =
        new("MergeRequest.Repository.NotFound", "Repository not found.");

    public static readonly DomainError NotFound =
        new("MergeRequest.NotFound", "Merge request not found.");

    public static readonly DomainError GitProviderUnauthorized =
        new("MergeRequest.GitProvider.Unauthorized", "Git provider rejected the request. Check your access token in Settings.");
}
