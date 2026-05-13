using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.MergeRequests;

public sealed record MergeRequestReadModel(
    Guid Id,
    Guid RepositoryId,
    int ProviderIid,
    string Title,
    string SourceBranch,
    string TargetBranch,
    string AuthorUsername,
    string State,
    string PlatformStatus,
    string ProjectPath,
    bool IsApproved)
{
    public static MergeRequestReadModel From(MergeRequest mr, Repository repo)
    {
        ArgumentNullException.ThrowIfNull(mr);
        ArgumentNullException.ThrowIfNull(repo);
        return new(
            mr.Id,
            mr.RepositoryId,
            mr.ProviderIid,
            mr.Title,
            mr.SourceBranch,
            mr.TargetBranch,
            mr.AuthorUsername,
            mr.Status.GetType().Name,
            mr.PlatformStatus.GetType().Name,
            repo.ProjectPath,
            mr.IsApproved);
    }
}
