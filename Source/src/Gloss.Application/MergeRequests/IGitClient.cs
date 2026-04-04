namespace Gloss.Application.MergeRequests;

public interface IGitClient
{
    Task<IReadOnlyList<MergeRequestData>> GetOpenMergeRequestsAsync(string projectPath, CancellationToken cancellationToken);
}
