namespace Gloss.Application.MergeRequests;

public sealed record MergeRequestData(
    int Iid,
    string Title,
    string? Description,
    string SourceBranch,
    string TargetBranch,
    string AuthorUsername,
    string Diff,
    string? BaseSha,
    string? HeadSha,
    string? StartSha);
