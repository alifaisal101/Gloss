namespace Gloss.Infrastructure.Reviews.Anthropic;

internal sealed record ClaudeMessage(string Role, IReadOnlyList<IClaudeContent> Content);
