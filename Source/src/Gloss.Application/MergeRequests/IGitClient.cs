namespace Gloss.Application.MergeRequests;

public interface IGitClient
{
    Task<IReadOnlyList<MergeRequestData>> GetOpenMergeRequestsAsync(string projectPath, CancellationToken cancellationToken);

    Task<IReadOnlyList<MrCommitData>> GetCommitsAsync(string projectPath, int mrIid, CancellationToken cancellationToken);

    Task<MrShasData?> GetMrShasAsync(string projectPath, int mrIid, CancellationToken cancellationToken);

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
