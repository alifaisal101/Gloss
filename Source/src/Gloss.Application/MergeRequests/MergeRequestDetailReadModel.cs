using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.MergeRequests;

public sealed record MergeRequestDetailReadModel(
    Guid Id,
    string Title,
    string State,
    string PlatformStatus,
    DateTimeOffset? PlatformStatusOccurredAt,
    string? PlatformStatusByUsername,
    string ProjectPath,
    string SourceBranch,
    string TargetBranch,
    string Diff,
    bool HasShas,
    bool IsApproved,
    IReadOnlyList<DraftCommentReadModel> Comments,
    IReadOnlyList<MrCommitReadModel> Commits,
    IReadOnlyList<PlatformCommentReadModel> PlatformComments)
{
    public static MergeRequestDetailReadModel From(
        MergeRequest mr,
        Repository repo,
        IReadOnlyList<DraftComment> comments,
        IReadOnlyList<MrCommit> commits,
        IReadOnlyList<PlatformCommentReadModel> platformComments)
    {
        ArgumentNullException.ThrowIfNull(mr);
        ArgumentNullException.ThrowIfNull(repo);
        ArgumentNullException.ThrowIfNull(comments);
        ArgumentNullException.ThrowIfNull(commits);
        ArgumentNullException.ThrowIfNull(platformComments);

        var (platformOccurredAt, platformBy) = mr.PlatformStatus switch
        {
            PlatformMrStatus.Closed c => ((DateTimeOffset?)c.OccurredAt, c.ByUsername),
            PlatformMrStatus.Merged m => (m.OccurredAt, m.ByUsername),
            _ => (null, null)
        };

        return new(
            mr.Id,
            mr.Title,
            mr.Status.GetType().Name,
            mr.PlatformStatus.GetType().Name,
            platformOccurredAt,
            platformBy,
            repo.ProjectPath,
            mr.SourceBranch,
            mr.TargetBranch,
            mr.Diff,
            mr.BaseSha is not null && mr.HeadSha is not null && mr.StartSha is not null,
            mr.IsApproved,
            comments.Select(DraftCommentReadModel.From).ToList(),
            commits.Select(MrCommitReadModel.From).ToList(),
            platformComments);
    }
}
