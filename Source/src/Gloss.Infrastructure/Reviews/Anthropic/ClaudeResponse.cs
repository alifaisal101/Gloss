namespace Gloss.Infrastructure.Reviews.Anthropic;

internal sealed record ClaudeResponse(string StopReason, IReadOnlyList<IClaudeContent> Content);
