namespace Gloss.Application.MergeRequests;

public interface IGitClient
{
    Task<IReadOnlyList<MergeRequestData>> GetOpenMergeRequestsAsync(string projectPath, CancellationToken cancellationToken);

    Task<IReadOnlyList<MrCommitData>> GetCommitsAsync(string projectPath, int mrIid, CancellationToken cancellationToken);

    Task<MrShasData?> GetMrShasAsync(string projectPath, int mrIid, CancellationToken cancellationToken);

    Task<PlatformMrStatusData> GetMergeRequestStatusAsync(string projectPath, int mrIid, CancellationToken cancellationToken);

    Task<ApprovalStatusData> GetApprovalStatusAsync(string projectPath, int mrIid, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformCommentData>> GetMrDiscussionsAsync(string projectPath, int mrIid, CancellationToken cancellationToken);

    Task PublishCommentAsync(
        string projectPath,
        int mrIid,
        string? baseSha,
        string? headSha,
        string? startSha,
        string filePath,
        int line,
        string body,
        CancellationToken cancellationToken);
}
