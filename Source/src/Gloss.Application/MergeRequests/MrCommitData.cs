namespace Gloss.Application.MergeRequests;

public sealed record MrCommitData(string Sha, string Title, string AuthorName, string Diff);
