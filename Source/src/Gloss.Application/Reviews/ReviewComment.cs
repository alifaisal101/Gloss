namespace Gloss.Application.Reviews;

public sealed record ReviewComment(string FilePath, int Line, string Body, string? Reasoning);
