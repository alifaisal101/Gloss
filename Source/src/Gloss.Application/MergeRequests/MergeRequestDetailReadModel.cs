using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.MergeRequests;

public sealed record MergeRequestDetailReadModel(
    Guid Id,
    string Title,
    string State,
    string ProjectPath,
    string SourceBranch,
    string TargetBranch,
    string Diff,
    bool HasShas,
    IReadOnlyList<DraftCommentReadModel> Comments,
    IReadOnlyList<MrCommitReadModel> Commits)
{
    public static MergeRequestDetailReadModel From(
        MergeRequest mr,
        Repository repo,
        IReadOnlyList<DraftComment> comments,
        IReadOnlyList<MrCommit> commits)
    {
        ArgumentNullException.ThrowIfNull(mr);
        ArgumentNullException.ThrowIfNull(repo);
        ArgumentNullException.ThrowIfNull(comments);
        ArgumentNullException.ThrowIfNull(commits);
        return new(
            mr.Id,
            mr.Title,
            mr.State.ToString(),
            repo.ProjectPath,
            mr.SourceBranch,
            mr.TargetBranch,
            mr.Diff,
            mr.BaseSha is not null && mr.HeadSha is not null && mr.StartSha is not null,
            comments.Select(DraftCommentReadModel.From).ToList(),
            commits.Select(MrCommitReadModel.From).ToList());
    }
}
