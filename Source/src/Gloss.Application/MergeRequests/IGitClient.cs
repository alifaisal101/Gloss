namespace Gloss.Application.MergeRequests;

public interface IGitClient
{
    Task<IReadOnlyList<MergeRequestData>> GetOpenMergeRequestsAsync(string projectPath, CancellationToken cancellationToken);

    Task PublishCommentAsync(
        string projectPath,
        int mrIid,
        string baseSha,
        string headSha,
        string startSha,
        string filePath,
        int line,
        string body,
        CancellationToken cancellationToken);
}
