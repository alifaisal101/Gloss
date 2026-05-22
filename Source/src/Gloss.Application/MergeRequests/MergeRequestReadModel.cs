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
    bool IsApproved,
    string? ApprovedByUsername,
    DateTimeOffset? ApprovedAt)
{
    public static MergeRequestReadModel From(MergeRequest mr, Repository repo, MrReview? review)
    {
        ArgumentNullException.ThrowIfNull(mr);
        ArgumentNullException.ThrowIfNull(repo);
        var approved = mr.Approval as ApprovalStatus.Approved;
        return new(
            mr.Id,
            mr.RepositoryId,
            mr.ProviderIid,
            mr.Title,
            mr.SourceBranch,
            mr.TargetBranch,
            mr.AuthorUsername,
            review?.Status.GetType().Name ?? "Pending",
            mr.PlatformStatus.GetType().Name,
            repo.ProjectPath,
            approved is not null,
            approved?.ByUsername,
            approved?.ApprovedAt);
    }
}
