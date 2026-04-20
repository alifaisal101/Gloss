namespace Gloss.Application.Reviews;

public sealed record ReviewContext(string Diff, string RepoPath, string? Projection = null);
