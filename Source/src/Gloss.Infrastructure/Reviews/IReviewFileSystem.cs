namespace Gloss.Infrastructure.Reviews;

internal interface IReviewFileSystem
{
    string? ReadFile(string repoPath, string relativePath);
    IReadOnlyList<string> ListDirectory(string repoPath, string relativePath);
    IReadOnlyList<string> SearchCode(string repoPath, string pattern);
}
